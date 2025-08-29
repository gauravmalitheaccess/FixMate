import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientTestingModule } from '@angular/common/http/testing';
import { of, throwError } from 'rxjs';

import { ExcelDownloadDialogComponent, ExcelDownloadDialogData } from './excel-download-dialog.component';
import { ExcelDownloadService } from '../../services/excel-download.service';
import { ExcelExportRequest } from '../../models/excel-export.model';

describe('ExcelDownloadDialogComponent Integration', () => {
  let component: ExcelDownloadDialogComponent;
  let fixture: ComponentFixture<ExcelDownloadDialogComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<ExcelDownloadDialogComponent>>;
  let mockExcelService: jasmine.SpyObj<ExcelDownloadService>;

  const mockDialogData: ExcelDownloadDialogData = {
    defaultDateFrom: new Date('2024-01-01'),
    defaultDateTo: new Date('2024-01-07')
  };

  beforeEach(async () => {
    mockDialogRef = jasmine.createSpyObj('MatDialogRef', ['close']);
    mockExcelService = jasmine.createSpyObj('ExcelDownloadService', [
      'downloadExcel',
      'generateFileName',
      'triggerDownload'
    ]);

    await TestBed.configureTestingModule({
      imports: [
        ExcelDownloadDialogComponent,
        NoopAnimationsModule,
        HttpClientTestingModule
      ],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: mockDialogData },
        { provide: ExcelDownloadService, useValue: mockExcelService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ExcelDownloadDialogComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create with default data', () => {
    expect(component).toBeTruthy();
    expect(component.downloadForm.get('dateFrom')?.value).toEqual(mockDialogData.defaultDateFrom);
    expect(component.downloadForm.get('dateTo')?.value).toEqual(mockDialogData.defaultDateTo);
  });

  it('should set quick date ranges correctly', () => {
    const today = new Date();
    const expectedStartDate = new Date();
    expectedStartDate.setDate(today.getDate() - 7);

    component.setQuickRange(7);

    const formDateFrom = component.downloadForm.get('dateFrom')?.value;
    const formDateTo = component.downloadForm.get('dateTo')?.value;

    expect(formDateFrom.toDateString()).toBe(expectedStartDate.toDateString());
    expect(formDateTo.toDateString()).toBe(today.toDateString());
  });

  it('should validate date range correctly', () => {
    const today = new Date();
    const tomorrow = new Date();
    tomorrow.setDate(today.getDate() + 1);

    // Set invalid range (from > to)
    component.downloadForm.patchValue({
      dateFrom: tomorrow,
      dateTo: today
    });

    expect(component.downloadForm.hasError('dateRangeInvalid')).toBe(true);
    expect(component.getDateRangeError()).toBe('End date must be after start date');
  });

  it('should estimate file size based on date range', () => {
    const dateFrom = new Date('2024-01-01');
    const dateTo = new Date('2024-01-03'); // 3 days

    component.downloadForm.patchValue({
      dateFrom,
      dateTo
    });

    const estimate = component.getEstimatedFileSize();
    expect(estimate).toContain('KB'); // Should show KB for small ranges
  });

  it('should close dialog with request data on download', () => {
    const dateFrom = new Date('2024-01-01');
    const dateTo = new Date('2024-01-07');

    component.downloadForm.patchValue({
      dateFrom,
      dateTo,
      includeUnanalyzed: true
    });

    component.onDownload();

    expect(mockDialogRef.close).toHaveBeenCalledWith({
      dateFrom,
      dateTo,
      includeUnanalyzed: true
    } as ExcelExportRequest);
  });

  it('should not close dialog if form is invalid', () => {
    component.downloadForm.patchValue({
      dateFrom: null,
      dateTo: null
    });

    component.onDownload();

    expect(mockDialogRef.close).not.toHaveBeenCalled();
  });

  it('should close dialog without data on cancel', () => {
    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalledWith();
  });

  describe('Form Validation', () => {
    it('should require both dates', () => {
      component.downloadForm.patchValue({
        dateFrom: null,
        dateTo: null
      });

      expect(component.downloadForm.get('dateFrom')?.hasError('required')).toBe(true);
      expect(component.downloadForm.get('dateTo')?.hasError('required')).toBe(true);
      expect(component.downloadForm.valid).toBe(false);
    });

    it('should accept valid date range', () => {
      const dateFrom = new Date('2024-01-01');
      const dateTo = new Date('2024-01-07');

      component.downloadForm.patchValue({
        dateFrom,
        dateTo,
        includeUnanalyzed: false
      });

      expect(component.downloadForm.valid).toBe(true);
    });

    it('should prevent future dates', () => {
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);

      // The maxDate should prevent future dates
      expect(component.maxDate.getTime()).toBeLessThanOrEqual(new Date().getTime());
    });
  });

  describe('Quick Range Buttons', () => {
    it('should set today range correctly', () => {
      const today = new Date();
      const yesterday = new Date();
      yesterday.setDate(today.getDate() - 1);
      
      component.setQuickRange(1);
      
      const formDateFrom = component.downloadForm.get('dateFrom')?.value;
      const formDateTo = component.downloadForm.get('dateTo')?.value;
      
      expect(formDateFrom.toDateString()).toBe(yesterday.toDateString());
      expect(formDateTo.toDateString()).toBe(today.toDateString());
    });

    it('should set 30-day range correctly', () => {
      const today = new Date();
      const thirtyDaysAgo = new Date();
      thirtyDaysAgo.setDate(today.getDate() - 30);

      component.setQuickRange(30);

      const formDateFrom = component.downloadForm.get('dateFrom')?.value;
      const formDateTo = component.downloadForm.get('dateTo')?.value;

      expect(formDateFrom.toDateString()).toBe(thirtyDaysAgo.toDateString());
      expect(formDateTo.toDateString()).toBe(today.toDateString());
    });
  });
});