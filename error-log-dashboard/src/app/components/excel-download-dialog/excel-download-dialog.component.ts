import { Component, Inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { ExcelExportRequest } from '../../models/excel-export.model';

export interface ExcelDownloadDialogData {
  defaultDateFrom?: Date;
  defaultDateTo?: Date;
}

@Component({
  selector: 'app-excel-download-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatCheckboxModule,
    MatIconModule,
    MatProgressSpinnerModule
  ],
  templateUrl: './excel-download-dialog.component.html',
  styleUrl: './excel-download-dialog.component.scss'
})
export class ExcelDownloadDialogComponent implements OnInit {
  downloadForm: FormGroup;
  isDownloading = false;
  maxDate = new Date();

  constructor(
    private fb: FormBuilder,
    private dialogRef: MatDialogRef<ExcelDownloadDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ExcelDownloadDialogData
  ) {
    this.downloadForm = this.fb.group({
      dateFrom: [null, Validators.required],
      dateTo: [null, Validators.required],
      includeUnanalyzed: [false]
    });
  }

  ngOnInit(): void {
    // Set default dates if provided
    if (this.data?.defaultDateFrom) {
      this.downloadForm.patchValue({
        dateFrom: this.data.defaultDateFrom
      });
    }

    if (this.data?.defaultDateTo) {
      this.downloadForm.patchValue({
        dateTo: this.data.defaultDateTo
      });
    }

    // If no defaults provided, set to last 7 days
    if (!this.data?.defaultDateFrom && !this.data?.defaultDateTo) {
      const today = new Date();
      const weekAgo = new Date();
      weekAgo.setDate(today.getDate() - 7);

      this.downloadForm.patchValue({
        dateFrom: weekAgo,
        dateTo: today
      });
    }

    // Add custom validator for date range
    this.downloadForm.setValidators(this.dateRangeValidator.bind(this));
  }

  dateRangeValidator(control: AbstractControl) {
    const form = control as FormGroup;
    const dateFrom = form.get('dateFrom')?.value;
    const dateTo = form.get('dateTo')?.value;

    if (dateFrom && dateTo && dateFrom > dateTo) {
      return { dateRangeInvalid: true };
    }

    return null;
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onDownload(): void {
    if (this.downloadForm.valid) {
      const formValue = this.downloadForm.value;
      const request: ExcelExportRequest = {
        dateFrom: formValue.dateFrom,
        dateTo: formValue.dateTo,
        includeUnanalyzed: formValue.includeUnanalyzed
      };

      this.dialogRef.close(request);
    }
  }

  setQuickRange(days: number): void {
    const today = new Date();
    const startDate = new Date();
    startDate.setDate(today.getDate() - days);

    this.downloadForm.patchValue({
      dateFrom: startDate,
      dateTo: today
    });
  }

  getDateRangeError(): string | null {
    if (this.downloadForm.hasError('dateRangeInvalid')) {
      return 'End date must be after start date';
    }
    return null;
  }

  getEstimatedFileSize(): string {
    const dateFrom = this.downloadForm.get('dateFrom')?.value;
    const dateTo = this.downloadForm.get('dateTo')?.value;

    if (!dateFrom || !dateTo) {
      return 'Select dates to estimate file size';
    }

    const daysDiff = Math.ceil((dateTo.getTime() - dateFrom.getTime()) / (1000 * 60 * 60 * 24));
    const estimatedLogs = daysDiff * 100; // Assume 100 logs per day on average
    const estimatedSizeKB = Math.ceil(estimatedLogs * 0.5); // Assume 0.5KB per log entry

    if (estimatedSizeKB < 1024) {
      return `~${estimatedSizeKB} KB`;
    } else {
      return `~${(estimatedSizeKB / 1024).toFixed(1)} MB`;
    }
  }
}
