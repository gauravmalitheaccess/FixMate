import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar } from '@angular/material/snack-bar';

import { ErrorLog } from '../../models/error-log.model';

@Component({
  selector: 'app-stack-trace-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule
  ],
  template: `
    <div class="stack-trace-dialog">
      <div mat-dialog-title class="dialog-header">
        <div class="header-content">
          <mat-icon class="header-icon">bug_report</mat-icon>
          <div class="header-text">
            <h2>Stack Trace</h2>
            <span class="log-source">{{ data.source }}</span>
          </div>
        </div>
        <button mat-icon-button mat-dialog-close class="close-button">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div mat-dialog-content class="dialog-content">
        <div class="error-message">
          <h4>Error Message:</h4>
          <p class="message-text">{{ data.message }}</p>
        </div>

        <div class="stack-trace-section">
          <div class="section-header">
            <h4>Stack Trace:</h4>
            <button 
              mat-stroked-button 
              (click)="copyStackTrace()"
              class="copy-button">
              <mat-icon>content_copy</mat-icon>
              Copy
            </button>
          </div>
          
          <div class="stack-trace-container">
            <pre class="stack-trace-text">{{ data.stackTrace || 'No stack trace available' }}</pre>
          </div>
        </div>
      </div>

      <div mat-dialog-actions class="dialog-actions">
        <button mat-button mat-dialog-close>Close</button>
        <button 
          mat-raised-button 
          color="primary"
          (click)="copyStackTrace()"
          [disabled]="!data.stackTrace">
          <mat-icon>content_copy</mat-icon>
          Copy Stack Trace
        </button>
      </div>
    </div>
  `,
  styles: [`
    .stack-trace-dialog {
      width: 100%;
      max-width: 900px;
      max-height: 80vh;
    }

    .dialog-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 0;
      margin: 0;
    }

    .header-content {
      display: flex;
      align-items: center;
      gap: 12px;
    }

    .header-icon {
      color: #f44336;
      font-size: 28px;
      width: 28px;
      height: 28px;
    }

    .header-text h2 {
      margin: 0;
      font-size: 20px;
      font-weight: 500;
    }

    .log-source {
      font-size: 12px;
      color: #666;
      font-family: monospace;
    }

    .close-button {
      margin-left: auto;
    }

    .dialog-content {
      padding: 0 24px;
      max-height: 60vh;
      overflow-y: auto;
    }

    .error-message {
      margin-bottom: 20px;
      padding: 16px;
      background: #fff3e0;
      border-left: 4px solid #ff9800;
      border-radius: 4px;
    }

    .error-message h4 {
      margin: 0 0 8px 0;
      color: #e65100;
      font-size: 14px;
      font-weight: 500;
    }

    .message-text {
      margin: 0;
      font-family: 'Roboto Mono', monospace;
      font-size: 13px;
      line-height: 1.4;
      word-break: break-word;
    }

    .stack-trace-section h4 {
      margin: 0 0 12px 0;
      color: #333;
      font-size: 14px;
      font-weight: 500;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 12px;
    }

    .copy-button {
      font-size: 12px;
    }

    .stack-trace-container {
      background: #1e1e1e;
      border-radius: 8px;
      padding: 16px;
      overflow-x: auto;
      max-height: 400px;
      overflow-y: auto;
    }

    .stack-trace-text {
      margin: 0;
      color: #d4d4d4;
      font-family: 'Roboto Mono', monospace;
      font-size: 12px;
      line-height: 1.4;
      white-space: pre;
    }

    .dialog-actions {
      padding: 16px 24px;
      gap: 12px;
    }

    @media (max-width: 600px) {
      .stack-trace-dialog {
        max-width: 95vw;
      }
      
      .section-header {
        flex-direction: column;
        align-items: flex-start;
        gap: 8px;
      }
    }
  `]
})
export class StackTraceDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<StackTraceDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ErrorLog,
    private snackBar: MatSnackBar
  ) {}

  copyStackTrace(): void {
    if (this.data.stackTrace) {
      navigator.clipboard.writeText(this.data.stackTrace).then(() => {
        this.snackBar.open('Stack trace copied to clipboard', 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
      }).catch(() => {
        this.snackBar.open('Failed to copy stack trace', 'Close', {
          duration: 3000,
          panelClass: ['error-snackbar']
        });
      });
    }
  }
}