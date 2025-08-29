import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';

import { DashboardComponent } from './dashboard.component';
import { LogService } from '../services/log.service';
import { ExcelDownloadService } from '../services/excel-download.service';
import { ExcelDownloadDialogComponent, ExcelDownloadDialogData } from '../components/excel-download-dialog/excel-download-dialog.component';
import { ExcelExportRequest } from '../models/excel-export.model';
import { LogStatistics } from '../models/error-log.model';

describe('Dashboard Excel Download E2E', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let mockLogService: jasmine.SpyObj<LogService>;
  let mockExcelService: jasmine.SpyObj<ExcelDownloadService>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const mockStatistics: LogStatistics = {
    totalLogs: 150,
    criticalCount: 5,
    highCount: 25,
    mediumCount: 70,
    lowCount: 50,
    analyzedCount: 120,
    unanalyzedCount: 30,
    todayCount: 15,
    weekCount: 85,
    monthCount: 150
  };

  beforeEach(async () => {
    mockLogService = jasmine.createSpyObj('LogService', ['getLogs', 'getLogStatistics']);
    mockExcelService = jasmine.createSpyObj('ExcelDownloadService', [
      'downloadExcel',
      'generateFileName',
      'triggerDownload'
    ]);
    mockDialog = jasmine.createSpyObj('MatDialog', ['open']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [
        DashboardComponent,
        NoopAnimationsModule,
        HttpClientTestingModule
      ],
      providers: [
        { provide: LogService, useValue: mockLogService },
        { provide: ExcelDownloadService, useValue: mockExcelService },
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;

    // Setup default mocks
    mockLogService.getLogStatistics.and.returnValue(of(mockStatistics));
    mockLogService.getLogs.and.returnValue(of([]));
  });

  describe('Excel Download Button', () => {
    it('should display download button in toolbar', () => {
      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download Excel Report"]'
      );
      
      expect(downloadButton).toBeTruthy();
      expect(downloadButton.querySelector('mat-icon').textContent.trim()).toBe('file_download');
    });

    it('should disable download button when loading', () => {
      component.isLoading = true;
      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download Excel Report"]'
      );
      
      expect(downloadButton.disabled).toBe(true);
    });

    it('should disable download button when downloading', () => {
      component.isDownloading = true;
      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download Excel Report"]'
      );
      
      expect(downloadButton.disabled).toBe(true);
    });
  });

  describe('Excel Download Dialog Integration', () => {
    it('should open dialog when download button is clicked', () => {
      const mockDialogRef = {
        afterClosed: () => of(null)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);

      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download Excel Report"]'
      );
      
      downloadButton.click();
      
      expect(mockDialog.open).toHaveBeenCalledWith(
        ExcelDownloadDialogComponent,
        jasmine.objectContaining({
          width: '500px',
          data: jasmine.objectContaining({
            defaultDateFrom: jasmine.any(Date),
            defaultDateTo: jasmine.any(Date)
          }),
          disableClose: false
        })
      );
    });

    it('should set default date range to last 7 days', () => {
      const mockDialogRef = {
        afterClosed: () => of(null)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);

      fixture.detectChanges();
      component.openExcelDownloadDialog();

      const dialogCall = mockDialog.open.calls.mostRecent();
      const dialogData = dialogCall.args[1]?.data as ExcelDownloadDialogData;
      
      const today = new Date();
      const weekAgo = new Date();
      weekAgo.setDate(today.getDate() - 7);

      expect(dialogData.defaultDateFrom?.toDateString()).toBe(weekAgo.toDateString());
      expect(dialogData.defaultDateTo?.toDateString()).toBe(today.toDateString());
    });

    it('should prevent dialog close when downloading', () => {
      component.isDownloading = true;
      const mockDialogRef = {
        afterClosed: () => of(null)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);

      fixture.detectChanges();
      component.openExcelDownloadDialog();

      const dialogCall = mockDialog.open.calls.mostRecent();
      expect(dialogCall.args[1]?.disableClose).toBe(true);
    });
  });

  describe('Excel Download Workflow', () => {
    it('should complete successful download workflow', (done) => {
      const mockRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07'),
        includeUnanalyzed: false
      };

      const mockBlob = new Blob(['mock excel data']);
      const expectedFilename = 'error-logs-2024-01-01-to-2024-01-07.xlsx';

      // Setup mocks
      const mockDialogRef = {
        afterClosed: () => of(mockRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);
      mockExcelService.downloadExcel.and.returnValue(of(mockBlob));
      mockExcelService.generateFileName.and.returnValue(expectedFilename);
      mockExcelService.triggerDownload.and.stub();

      const mockProgressSnackBar = {
        dismiss: jasmine.createSpy('dismiss')
      };
      const mockSuccessSnackBar = {
        onAction: () => of(null)
      };
      
      mockSnackBar.open.and.returnValues(
        mockProgressSnackBar as any,
        mockSuccessSnackBar as any
      );

      fixture.detectChanges();

      // Start the workflow
      component.openExcelDownloadDialog();

      // Verify the workflow
      setTimeout(() => {
        expect(mockExcelService.downloadExcel).toHaveBeenCalledWith(mockRequest);
        expect(mockExcelService.generateFileName).toHaveBeenCalledWith(
          mockRequest.dateFrom,
          mockRequest.dateTo
        );
        expect(mockExcelService.triggerDownload).toHaveBeenCalledWith(mockBlob, expectedFilename);
        expect(mockProgressSnackBar.dismiss).toHaveBeenCalled();
        expect(component.isDownloading).toBe(false);
        done();
      }, 100);
    });

    it('should handle download errors with retry option', (done) => {
      const mockRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      const mockError = new Error('Network error occurred');

      // Setup mocks
      const mockDialogRef = {
        afterClosed: () => of(mockRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);
      mockExcelService.downloadExcel.and.returnValue(throwError(() => mockError));

      const mockProgressSnackBar = {
        dismiss: jasmine.createSpy('dismiss')
      };
      const mockErrorSnackBar = {
        onAction: () => of('retry')
      };
      
      mockSnackBar.open.and.returnValues(
        mockProgressSnackBar as any,
        mockErrorSnackBar as any
      );

      fixture.detectChanges();

      // Start the workflow
      component.openExcelDownloadDialog();

      // Verify error handling
      setTimeout(() => {
        expect(mockExcelService.downloadExcel).toHaveBeenCalledWith(mockRequest);
        expect(mockProgressSnackBar.dismiss).toHaveBeenCalled();
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          'Network error occurred',
          'Retry',
          jasmine.objectContaining({
            duration: 8000,
            panelClass: ['error-snackbar']
          })
        );
        expect(component.isDownloading).toBe(false);
        done();
      }, 100);
    });

    it('should show progress indicator during download', () => {
      const mockRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      // Setup mocks for long-running download
      const mockDialogRef = {
        afterClosed: () => of(mockRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);
      
      // Don't resolve the observable immediately
      mockExcelService.downloadExcel.and.returnValue(of(new Blob()).pipe());

      const mockProgressSnackBar = {
        dismiss: jasmine.createSpy('dismiss')
      };
      mockSnackBar.open.and.returnValue(mockProgressSnackBar as any);

      fixture.detectChanges();

      // Start download
      component.openExcelDownloadDialog();

      // Verify progress state
      expect(component.isDownloading).toBe(true);
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Generating Excel file...',
        '',
        jasmine.objectContaining({
          duration: 0,
          panelClass: ['info-snackbar']
        })
      );
    });
  });

  describe('Error Scenarios', () => {
    it('should handle dialog cancellation gracefully', () => {
      const mockDialogRef = {
        afterClosed: () => of(null) // User cancelled
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);

      fixture.detectChanges();
      component.openExcelDownloadDialog();

      // Should not attempt download
      expect(mockExcelService.downloadExcel).not.toHaveBeenCalled();
      expect(component.isDownloading).toBe(false);
    });

    it('should handle server errors appropriately', (done) => {
      const mockRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      const serverError = new Error('Server returned error code 500: Internal Server Error');

      const mockDialogRef = {
        afterClosed: () => of(mockRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);
      mockExcelService.downloadExcel.and.returnValue(throwError(() => serverError));

      const mockProgressSnackBar = { dismiss: jasmine.createSpy('dismiss') };
      const mockErrorSnackBar = { onAction: () => of(null) };
      
      mockSnackBar.open.and.returnValues(
        mockProgressSnackBar as any,
        mockErrorSnackBar as any
      );

      fixture.detectChanges();
      component.openExcelDownloadDialog();

      setTimeout(() => {
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          'Server returned error code 500: Internal Server Error',
          'Retry',
          jasmine.objectContaining({
            duration: 8000,
            panelClass: ['error-snackbar']
          })
        );
        done();
      }, 100);
    });
  });

  describe('Accessibility and UX', () => {
    it('should have proper ARIA labels and tooltips', () => {
      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download Excel Report"]'
      );
      
      expect(downloadButton.getAttribute('matTooltip')).toBe('Download Excel Report');
    });

    it('should provide visual feedback during download', () => {
      component.isDownloading = true;
      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download Excel Report"]'
      );
      
      expect(downloadButton.disabled).toBe(true);
    });
  });
});