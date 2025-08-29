import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';

import { LogsComponent } from './logs.component';
import { LogService } from '../../services/log.service';
import { ExcelDownloadService } from '../../services/excel-download.service';
import { ExcelDownloadDialogComponent, ExcelDownloadDialogData } from '../../components/excel-download-dialog/excel-download-dialog.component';
import { ExcelExportRequest } from '../../models/excel-export.model';
import { ErrorLog, LogFilter } from '../../models/error-log.model';

describe('Logs Page Excel Download E2E', () => {
  let component: LogsComponent;
  let fixture: ComponentFixture<LogsComponent>;
  let mockLogService: jasmine.SpyObj<LogService>;
  let mockExcelService: jasmine.SpyObj<ExcelDownloadService>;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const mockLogs: ErrorLog[] = [
    {
      id: '1',
      timestamp: new Date('2024-01-15T10:00:00Z'),
      source: 'WebApp',
      message: 'Database connection failed',
      stackTrace: 'Error at line 123',
      severity: 'High',
      priority: 'High',
      aiReasoning: 'Critical database issue',
      analyzedAt: new Date('2024-01-15T10:05:00Z'),
      isAnalyzed: true
    },
    {
      id: '2',
      timestamp: new Date('2024-01-15T11:00:00Z'),
      source: 'API',
      message: 'Validation error',
      stackTrace: 'Error at line 456',
      severity: 'Medium',
      priority: 'Medium',
      aiReasoning: 'Input validation issue',
      analyzedAt: new Date('2024-01-15T11:05:00Z'),
      isAnalyzed: true
    }
  ];

  beforeEach(async () => {
    mockLogService = jasmine.createSpyObj('LogService', ['getLogs']);
    mockExcelService = jasmine.createSpyObj('ExcelDownloadService', [
      'downloadExcel',
      'generateFileName',
      'triggerDownload'
    ]);
    mockDialog = jasmine.createSpyObj('MatDialog', ['open']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [
        LogsComponent,
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

    fixture = TestBed.createComponent(LogsComponent);
    component = fixture.componentInstance;

    // Setup default mocks
    mockLogService.getLogs.and.returnValue(of(mockLogs));
  });

  describe('Excel Download Button', () => {
    it('should display download button in page header', () => {
      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download filtered logs as Excel"]'
      );
      
      expect(downloadButton).toBeTruthy();
      expect(downloadButton.textContent.trim()).toContain('Download Excel');
      expect(downloadButton.querySelector('mat-icon').textContent.trim()).toBe('file_download');
    });

    it('should disable download button when loading', () => {
      component.isLoading = true;
      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download filtered logs as Excel"]'
      );
      
      expect(downloadButton.disabled).toBe(true);
    });

    it('should disable download button when downloading', () => {
      component.isDownloading = true;
      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download filtered logs as Excel"]'
      );
      
      expect(downloadButton.disabled).toBe(true);
    });
  });

  describe('Dialog Integration with Current Filters', () => {
    it('should pass current filter dates as dialog defaults', () => {
      const filterDateFrom = new Date('2024-01-10');
      const filterDateTo = new Date('2024-01-20');
      
      component.currentFilter = {
        dateFrom: filterDateFrom,
        dateTo: filterDateTo,
        severity: 'High'
      };

      const mockDialogRef = {
        afterClosed: () => of(null)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);

      fixture.detectChanges();
      component.openExcelDownloadDialog();

      const dialogCall = mockDialog.open.calls.mostRecent();
      const dialogData = dialogCall.args[1]?.data as ExcelDownloadDialogData;
      
      expect(dialogData.defaultDateFrom).toBe(filterDateFrom);
      expect(dialogData.defaultDateTo).toBe(filterDateTo);
    });

    it('should handle undefined filter dates gracefully', () => {
      component.currentFilter = {
        severity: 'High'
        // No dates set
      };

      const mockDialogRef = {
        afterClosed: () => of(null)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);

      fixture.detectChanges();
      component.openExcelDownloadDialog();

      const dialogCall = mockDialog.open.calls.mostRecent();
      const dialogData = dialogCall.args[1]?.data as ExcelDownloadDialogData;
      
      expect(dialogData.defaultDateFrom).toBeUndefined();
      expect(dialogData.defaultDateTo).toBeUndefined();
    });
  });

  describe('Complete Download Workflow', () => {
    it('should complete successful download with filtered data context', (done) => {
      const mockRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-10'),
        dateTo: new Date('2024-01-20'),
        includeUnanalyzed: true
      };

      const mockBlob = new Blob(['filtered excel data']);
      const expectedFilename = 'error-logs-2024-01-10-to-2024-01-20.xlsx';

      // Setup current filter
      component.currentFilter = {
        dateFrom: new Date('2024-01-10'),
        dateTo: new Date('2024-01-20'),
        severity: 'High'
      };

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
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          `Excel file "${expectedFilename}" downloaded successfully`,
          'Close',
          jasmine.objectContaining({
            duration: 3000,
            panelClass: ['success-snackbar']
          })
        );
        expect(component.isDownloading).toBe(false);
        done();
      }, 100);
    });

    it('should handle download errors with retry functionality', (done) => {
      const mockRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      const mockError = new Error('Server error occurred');

      // Setup mocks
      const mockDialogRef = {
        afterClosed: () => of(mockRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);
      
      // First call fails, second call succeeds
      let callCount = 0;
      mockExcelService.downloadExcel.and.callFake(() => {
        callCount++;
        if (callCount === 1) {
          return throwError(() => mockError);
        } else {
          return of(new Blob(['retry success']));
        }
      });
      mockExcelService.generateFileName.and.returnValue('test-file.xlsx');
      mockExcelService.triggerDownload.and.stub();

      const mockProgressSnackBar = {
        dismiss: jasmine.createSpy('dismiss')
      };
      const mockErrorSnackBar = {
        onAction: () => of('retry')
      };
      const mockSuccessSnackBar = {
        onAction: () => of(null)
      };
      
      mockSnackBar.open.and.returnValues(
        mockProgressSnackBar as any,
        mockErrorSnackBar as any,
        mockProgressSnackBar as any,
        mockSuccessSnackBar as any
      );

      fixture.detectChanges();

      // Start the workflow
      component.openExcelDownloadDialog();

      // Verify error handling and retry
      setTimeout(() => {
        expect(mockExcelService.downloadExcel).toHaveBeenCalledTimes(2);
        expect(mockSnackBar.open).toHaveBeenCalledWith(
          'Server error occurred',
          'Retry',
          jasmine.objectContaining({
            duration: 8000,
            panelClass: ['error-snackbar']
          })
        );
        done();
      }, 200);
    });
  });

  describe('User Experience Features', () => {
    it('should show appropriate loading states', () => {
      const mockRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      const mockDialogRef = {
        afterClosed: () => of(mockRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);
      
      // Create a never-resolving observable to test loading state
      mockExcelService.downloadExcel.and.returnValue(new Promise(() => {}) as any);

      const mockProgressSnackBar = {
        dismiss: jasmine.createSpy('dismiss')
      };
      mockSnackBar.open.and.returnValue(mockProgressSnackBar as any);

      fixture.detectChanges();

      // Start download
      component.openExcelDownloadDialog();

      // Verify loading state
      expect(component.isDownloading).toBe(true);
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Generating Excel file...',
        '',
        jasmine.objectContaining({
          duration: 0,
          panelClass: ['info-snackbar']
        })
      );

      // Verify button is disabled
      fixture.detectChanges();
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download filtered logs as Excel"]'
      );
      expect(downloadButton.disabled).toBe(true);
    });

    it('should prevent dialog close during download', () => {
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

  describe('Error Handling Edge Cases', () => {
    it('should handle network connectivity issues', (done) => {
      const mockRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      const networkError = new Error('Unable to connect to the server. Please check your connection.');

      const mockDialogRef = {
        afterClosed: () => of(mockRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);
      mockExcelService.downloadExcel.and.returnValue(throwError(() => networkError));

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
          'Unable to connect to the server. Please check your connection.',
          'Retry',
          jasmine.objectContaining({
            duration: 8000,
            panelClass: ['error-snackbar']
          })
        );
        done();
      }, 100);
    });

    it('should handle empty data scenarios gracefully', (done) => {
      const mockRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      const noDataError = new Error('No data found for the selected date range');

      const mockDialogRef = {
        afterClosed: () => of(mockRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);
      mockExcelService.downloadExcel.and.returnValue(throwError(() => noDataError));

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
          'No data found for the selected date range',
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

  describe('Responsive Design and Accessibility', () => {
    it('should maintain proper button layout on mobile', () => {
      // Simulate mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768
      });

      fixture.detectChanges();
      
      const headerActions = fixture.debugElement.nativeElement.querySelector('.header-actions');
      expect(headerActions).toBeTruthy();
      
      const downloadButton = headerActions.querySelector('button');
      expect(downloadButton).toBeTruthy();
    });

    it('should have proper accessibility attributes', () => {
      fixture.detectChanges();
      
      const downloadButton = fixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download filtered logs as Excel"]'
      );
      
      expect(downloadButton.getAttribute('matTooltip')).toBe('Download filtered logs as Excel');
      // Button should be focusable
      expect(downloadButton.tabIndex).not.toBe(-1);
    });
  });
});