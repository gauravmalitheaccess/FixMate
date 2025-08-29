import { Component, Inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MAT_DIALOG_DATA, MatDialogRef, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ErrorLog } from '../../models/error-log.model';
import { PriorityIndicatorComponent } from '../priority-indicator/priority-indicator.component';

@Component({
  selector: 'app-log-details-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatChipsModule,
    MatDividerModule,
    MatTabsModule,
    MatTooltipModule,
    PriorityIndicatorComponent
  ],
  template: `
    <div class="log-details-dialog">
      <div mat-dialog-title class="dialog-header">
        <div class="header-content">
          <mat-icon class="header-icon">bug_report</mat-icon>
          <div class="header-text">
            <h2>Error Log Details</h2>
            <span class="log-id">ID: {{ data.id }}</span>
          </div>
        </div>
        <button mat-icon-button mat-dialog-close class="close-button">
          <mat-icon>close</mat-icon>
        </button>
      </div>

      <div mat-dialog-content class="dialog-content">
        <mat-tab-group>
          <!-- Overview Tab -->
          <mat-tab label="Overview">
            <div class="tab-content">
              <div class="info-section">
                <h3>Basic Information</h3>
                <div class="info-grid">
                  <div class="info-item">
                    <label>Timestamp:</label>
                    <span class="timestamp">
                      {{ data.timestamp | date:'MMM d, y, h:mm:ss a' }}
                    </span>
                  </div>
                  
                  <div class="info-item">
                    <label>Source:</label>
                    <span class="source">
                      <mat-icon class="source-icon">code</mat-icon>
                      {{ data.source }}
                    </span>
                  </div>

                  <div class="info-item">
                    <label>Severity:</label>
                    <mat-chip 
                      class="severity-chip"
                      [style.background-color]="getSeverityColor(data.severity)"
                      [style.color]="'white'">
                      <mat-icon matChipAvatar [style.color]="'white'">
                        {{ getSeverityIcon(data.severity) }}
                      </mat-icon>
                      {{ data.severity || 'Unknown' }}
                    </mat-chip>
                  </div>

                  <div class="info-item">
                    <label>Priority:</label>
                    <app-priority-indicator 
                      [priority]="data.priority" 
                      [aiReasoning]="data.aiReasoning"
                      size="medium">
                    </app-priority-indicator>
                  </div>

                  <div class="info-item">
                    <label>Analysis Status:</label>
                    <mat-chip 
                      *ngIf="data.isAnalyzed" 
                      class="status-chip analyzed">
                      <mat-icon matChipAvatar>check_circle</mat-icon>
                      Analyzed
                    </mat-chip>
                    <mat-chip 
                      *ngIf="!data.isAnalyzed" 
                      class="status-chip pending">
                      <mat-icon matChipAvatar>schedule</mat-icon>
                      Pending Analysis
                    </mat-chip>
                  </div>

                  <div class="info-item" *ngIf="data.analyzedAt">
                    <label>Analyzed At:</label>
                    <span class="timestamp">
                      {{ data.analyzedAt | date:'MMM d, y, h:mm:ss a' }}
                    </span>
                  </div>
                </div>
              </div>

              <mat-divider></mat-divider>

              <div class="message-section">
                <h3>Error Message</h3>
                <div class="message-content">
                  <pre class="message-text">{{ data.message }}</pre>
                </div>
              </div>

              <div class="ai-section" *ngIf="data.isAnalyzed && (data.aiReasoning || data.potentialFix)">
                <mat-divider></mat-divider>
                <h3>
                  <mat-icon>psychology</mat-icon>
                  AI Analysis
                </h3>
                <div class="ai-reasoning" *ngIf="data.aiReasoning">
                  <h4>Analysis Reasoning:</h4>
                  <p>{{ data.aiReasoning }}</p>
                </div>
                <div class="potential-fix-section" *ngIf="data.potentialFix">
                  <h4>
                    <mat-icon>lightbulb</mat-icon>
                    Potential Fix Suggestion:
                  </h4>
                  <div class="potential-fix-content">
                    <p>{{ data.potentialFix }}</p>
                  </div>
                </div>
              </div>
            </div>
          </mat-tab>

          <!-- Stack Trace Tab -->
          <mat-tab label="Stack Trace" [disabled]="!data.stackTrace">
            <div class="tab-content">
              <div class="stack-trace-section">
                <div class="section-header">
                  <h3>
                    <mat-icon>list</mat-icon>
                    Stack Trace
                  </h3>
                  <button 
                    mat-stroked-button 
                    (click)="copyStackTrace()"
                    class="copy-button">
                    <mat-icon>content_copy</mat-icon>
                    Copy
                  </button>
                </div>
                <div class="stack-trace-content">
                  <pre class="stack-trace-text">{{ data.stackTrace || 'No stack trace available' }}</pre>
                </div>
              </div>
            </div>
          </mat-tab>

          <!-- Raw Data Tab -->
          <mat-tab label="Raw Data">
            <div class="tab-content">
              <div class="raw-data-section">
                <div class="section-header">
                  <h3>
                    <mat-icon>data_object</mat-icon>
                    Raw Log Data
                  </h3>
                  <button 
                    mat-stroked-button 
                    (click)="copyRawData()"
                    class="copy-button">
                    <mat-icon>content_copy</mat-icon>
                    Copy JSON
                  </button>
                </div>
                <div class="raw-data-content">
                  <pre class="raw-data-text">{{ getRawDataJson() }}</pre>
                </div>
              </div>
            </div>
          </mat-tab>
        </mat-tab-group>
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
    .log-details-dialog {
      width: 100%;
      max-width: 800px;
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
      color: #1976d2;
      font-size: 28px;
      width: 28px;
      height: 28px;
    }

    .header-text h2 {
      margin: 0;
      font-size: 20px;
      font-weight: 500;
    }

    .log-id {
      font-size: 12px;
      color: #666;
      font-family: monospace;
    }

    .close-button {
      margin-left: auto;
    }

    .dialog-content {
      padding: 0;
      margin: 0;
      max-height: 70vh;
      overflow: hidden;
    }

    .tab-content {
      padding: 20px;
      max-height: 60vh;
      overflow-y: auto;
    }

    .info-section h3,
    .message-section h3,
    .ai-section h3,
    .stack-trace-section h3,
    .raw-data-section h3 {
      margin: 0 0 16px 0;
      color: #333;
      display: flex;
      align-items: center;
      gap: 8px;
    }

    .info-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 16px;
      margin-bottom: 20px;
    }

    .info-item {
      display: flex;
      flex-direction: column;
      gap: 4px;
    }

    .info-item label {
      font-weight: 500;
      color: #666;
      font-size: 12px;
      text-transform: uppercase;
      letter-spacing: 0.5px;
    }

    .timestamp {
      font-family: monospace;
      font-size: 14px;
    }

    .source {
      display: flex;
      align-items: center;
      gap: 6px;
      font-family: monospace;
      font-size: 14px;
    }

    .source-icon {
      font-size: 16px;
      width: 16px;
      height: 16px;
      color: #666;
    }

    .severity-chip {
      width: fit-content;
      font-weight: 500;
    }

    .status-chip {
      width: fit-content;
    }

    .status-chip.analyzed {
      background-color: #4caf50;
      color: white;
    }

    .status-chip.pending {
      background-color: #ff9800;
      color: white;
    }

    .message-content,
    .ai-reasoning {
      background: #f5f5f5;
      border-radius: 8px;
      padding: 16px;
      margin-top: 8px;
    }

    .message-text {
      margin: 0;
      white-space: pre-wrap;
      word-break: break-word;
      font-family: 'Roboto Mono', monospace;
      font-size: 13px;
      line-height: 1.4;
    }

    .ai-reasoning p {
      margin: 0;
      line-height: 1.5;
    }

    .ai-reasoning h4,
    .potential-fix-section h4 {
      margin: 0 0 8px 0;
      color: #333;
      font-size: 14px;
      font-weight: 500;
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .potential-fix-section {
      margin-top: 16px;
    }

    .potential-fix-content {
      background: #e8f5e8;
      border-left: 4px solid #4caf50;
      border-radius: 8px;
      padding: 16px;
      margin-top: 8px;
    }

    .potential-fix-content p {
      margin: 0;
      line-height: 1.5;
      color: #2e7d32;
    }

    .section-header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 16px;
    }

    .copy-button {
      font-size: 12px;
    }

    .stack-trace-content,
    .raw-data-content {
      background: #1e1e1e;
      border-radius: 8px;
      padding: 16px;
      overflow-x: auto;
    }

    .stack-trace-text,
    .raw-data-text {
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

    mat-divider {
      margin: 20px 0;
    }

    @media (max-width: 600px) {
      .info-grid {
        grid-template-columns: 1fr;
      }
      
      .log-details-dialog {
        max-width: 95vw;
      }
    }
  `]
})
export class LogDetailsDialogComponent {
  constructor(
    public dialogRef: MatDialogRef<LogDetailsDialogComponent>,
    @Inject(MAT_DIALOG_DATA) public data: ErrorLog
  ) {}

  getSeverityColor(severity: string): string {
    switch (severity?.toLowerCase()) {
      case 'critical': return '#d32f2f';
      case 'high': return '#f44336';
      case 'medium': return '#ff9800';
      case 'low': return '#4caf50';
      default: return '#9e9e9e';
    }
  }

  getSeverityIcon(severity: string): string {
    switch (severity?.toLowerCase()) {
      case 'critical': return 'dangerous';
      case 'high': return 'warning';
      case 'medium': return 'info';
      case 'low': return 'check_circle';
      default: return 'help_outline';
    }
  }

  copyStackTrace(): void {
    if (this.data.stackTrace) {
      navigator.clipboard.writeText(this.data.stackTrace).then(() => {
        // Could show a snackbar here
        console.log('Stack trace copied to clipboard');
      });
    }
  }

  copyRawData(): void {
    const rawData = this.getRawDataJson();
    navigator.clipboard.writeText(rawData).then(() => {
      console.log('Raw data copied to clipboard');
    });
  }

  getRawDataJson(): string {
    return JSON.stringify(this.data, null, 2);
  }
}
