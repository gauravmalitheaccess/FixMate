import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';

import { ExcelDownloadDialogComponent, ExcelDownloadDialogData } from './excel-download-dialog.component';

describe('ExcelDownloadDialogComponent', () => {
  let component: ExcelDownloadDialogComponent;
  let fixture: ComponentFixture<ExcelDownloadDialogComponent>;
  let mockDialogRef: jasmine.SpyObj<MatDialogRef<ExcelDownloadDialogComponent>>;

  const mockDialogData: ExcelDownloadDialogData = {
    defaultDateFrom: new Date('2024-01-01'),
    defaultDateTo: new Date('2024-01-07')
  };

  beforeEach(async () => {
    const dialogRefSpy = jasmine.createSpyObj('MatDialogRef', ['close']);

    await TestBed.configureTestingModule({
      imports: [
        ExcelDownloadDialogComponent,
        NoopAnimationsModule
      ],
      providers: [
        { provide: MatDialogRef, useValue: dialogRefSpy },
        { provide: MAT_DIALOG_DATA, useValue: mockDialogData }
      ]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(ExcelDownloadDialogComponent);
    component = fixture.componentInstance;
    mockDialogRef = TestBed.inject(MatDialogRef) as jasmine.SpyObj<MatDialogRef<ExcelDownloadDialogComponent>>;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize with default dates from dialog data', () => {
    expect(component.downloadForm.get('dateFrom')?.value).toEqual(mockDialogData.defaultDateFrom);
    expect(component.downloadForm.get('dateTo')?.value).toEqual(mockDialogData.defaultDateTo);
  });

  it('should initialize with last 7 days when no default dates provided', () => {
    // Create component without default dates
    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [
        ExcelDownloadDialogComponent,
        NoopAnimationsModule
      ],
      providers: [
        { provide: MatDialogRef, useValue: mockDialogRef },
        { provide: MAT_DIALOG_DATA, useValue: {} }
      ]
    });

    const newFixture = TestBed.createComponent(ExcelDownloadDialogComponent);
    const newComponent = newFixture.componentInstance;
    newFixture.detectChanges();

    const dateFrom = newComponent.downloadForm.get('dateFrom')?.value;
    const dateTo = newComponent.downloadForm.get('dateTo')?.value;

    expect(dateFrom).toBeInstanceOf(Date);
    expect(dateTo).toBeInstanceOf(Date);
    expect(dateTo.getTime() - dateFrom.getTime()).toBe(7 * 24 * 60 * 60 * 1000); // 7 days
  });

  it('should validate date range', () => {
    const dateFrom = new Date('2024-01-07');
    const dateTo = new Date('2024-01-01'); // Earlier than dateFrom

    component.downloadForm.patchValue({
      dateFrom: dateFrom,
      dateTo: dateTo
    });

    expect(component.downloadForm.hasError('dateRangeInvalid')).toBe(true);
    expect(component.getDateRangeError()).toBe('End date must be after start date');
  });

  it('should close dialog on cancel', () => {
    component.onCancel();
    expect(mockDialogRef.close).toHaveBeenCalledWith();
  });

  it('should close dialog with request data on download', () => {
    const dateFrom = new Date('2024-01-01');
    const dateTo = new Date('2024-01-07');
    
    component.downloadForm.patchValue({
      dateFrom: dateFrom,
      dateTo: dateTo,
      includeUnanalyzed: true
    });

    component.onDownload();

    expect(mockDialogRef.close).toHaveBeenCalledWith({
      dateFrom: dateFrom,
      dateTo: dateTo,
      includeUnanalyzed: true
    });
  });

  it('should not download if form is invalid', () => {
    component.downloadForm.patchValue({
      dateFrom: null,
      dateTo: null
    });

    component.onDownload();

    expect(mockDialogRef.close).not.toHaveBeenCalled();
  });

  it('should set quick date ranges correctly', () => {
    const today = new Date();
    
    component.setQuickRange(7);
    
    const dateFrom = component.downloadForm.get('dateFrom')?.value;
    const dateTo = component.downloadForm.get('dateTo')?.value;

    expect(dateTo.toDateString()).toBe(today.toDateString());
    expect(dateTo.getTime() - dateFrom.getTime()).toBe(7 * 24 * 60 * 60 * 1000);
  });

  it('should estimate file size based on date range', () => {
    component.downloadForm.patchValue({
      dateFrom: new Date('2024-01-01'),
      dateTo: new Date('2024-01-08') // 7 days
    });

    const estimate = component.getEstimatedFileSize();
    expect(estimate).toContain('KB'); // Should be in KB for small ranges
  });

  it('should show file size estimate in MB for large ranges', () => {
    component.downloadForm.patchValue({
      dateFrom: new Date('2024-01-01'),
      dateTo: new Date('2024-02-01') // ~31 days
    });

    const estimate = component.getEstimatedFileSize();
    expect(estimate).toContain('MB'); // Should be in MB for large ranges
  });

  it('should show message when no dates selected', () => {
    component.downloadForm.patchValue({
      dateFrom: null,
      dateTo: null
    });

    const estimate = component.getEstimatedFileSize();
    expect(estimate).toBe('Select dates to estimate file size');
  });

  it('should render quick range buttons', () => {
    const buttons = fixture.nativeElement.querySelectorAll('.range-button');
    expect(buttons.length).toBe(3);
    expect(buttons[0].textContent.trim()).toBe('Today');
    expect(buttons[1].textContent.trim()).toBe('Last 7 Days');
    expect(buttons[2].textContent.trim()).toBe('Last 30 Days');
  });

  it('should render form fields', () => {
    const dateFields = fixture.nativeElement.querySelectorAll('.date-field');
    const checkbox = fixture.nativeElement.querySelector('mat-checkbox');
    
    expect(dateFields.length).toBe(2);
    expect(checkbox).toBeTruthy();
  });

  it('should disable download button when form is invalid', () => {
    component.downloadForm.patchValue({
      dateFrom: null,
      dateTo: null
    });
    fixture.detectChanges();

    const downloadButton = fixture.nativeElement.querySelector('button[color="primary"]');
    expect(downloadButton.disabled).toBe(true);
  });
});
