import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSliderModule } from '@angular/material/slider';
import { MatIconModule } from '@angular/material/icon';

export interface LogGeneratorRequest {
  count: number;
  severity?: string;
  hoursBack: number;
}

export interface LogGeneratorDialogData {
  defaultCount?: number;
  defaultSeverity?: string;
  defaultHoursBack?: number;
}

@Component({
  selector: 'app-log-generator-dialog',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSliderModule,
    MatIconModule
  ],
  templateUrl: './log-generator-dialog.component.html',
  styleUrl: './log-generator-dialog.component.scss'
})
export class LogGeneratorDialogComponent {
  count: number;
  severity: string;
  hoursBack: number;

  severityOptions = [
    { value: '', label: 'Mixed (All Severities)' },
    { value: 'Critical', label: 'Critical' },
    { value: 'High', label: 'High' },
    { value: 'Medium', label: 'Medium' },
    { value: 'Low', label: 'Low' }
  ];

  constructor(
    public dialogRef: MatDialogRef<LogGeneratorDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: LogGeneratorDialogData
  ) {
    this.count = data.defaultCount || 10;
    this.severity = data.defaultSeverity || '';
    this.hoursBack = data.defaultHoursBack || 24;
  }

  onCancel(): void {
    this.dialogRef.close();
  }

  onGenerate(): void {
    const request: LogGeneratorRequest = {
      count: this.count,
      severity: this.severity || undefined,
      hoursBack: this.hoursBack
    };
    
    this.dialogRef.close(request);
  }

  isValid(): boolean {
    return this.count >= 1 && this.count <= 100 && 
           this.hoursBack >= 1 && this.hoursBack <= 168;
  }
}