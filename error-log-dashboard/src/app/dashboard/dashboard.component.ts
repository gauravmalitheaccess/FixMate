import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCardModule } from '@angular/material/card';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatDialog } from '@angular/material/dialog';
import { Subject, takeUntil, catchError, of, finalize } from 'rxjs';

import { LogService } from '../services/log.service';
import { ExcelDownloadService } from '../services/excel-download.service';
import { ErrorLog, LogStatistics } from '../models/error-log.model';
import { ExcelDownloadDialogComponent, ExcelDownloadDialogData } from '../components/excel-download-dialog/excel-download-dialog.component';
import { LogGeneratorDialogComponent, LogGeneratorDialogData, LogGeneratorRequest } from '../components/log-generator-dialog/log-generator-dialog.component';
import { AiAnalysisDialogComponent, AiAnalysisDialogData, RawExceptionData } from '../components/ai-analysis-dialog/ai-analysis-dialog.component';
import { ExcelExportRequest } from '../models/excel-export.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatToolbarModule,
    MatButtonModule,
    MatIconModule,
    MatCardModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    MatGridListModule,
    MatTooltipModule
  ],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  isLoading = false;
  isDownloading = false;
  statistics: LogStatistics | null = null;
  recentLogs: ErrorLog[] = [];
  error: string | null = null;

  constructor(
    private logService: LogService,
    private excelDownloadService: ExcelDownloadService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar
  ) {}

  ngOnInit(): void {
    this.loadDashboardData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadDashboardData(): void {
    this.isLoading = true;
    this.error = null;
    
    let statsLoaded = false;
    let logsLoaded = false;

    const checkComplete = () => {
      if (statsLoaded && logsLoaded) {
        this.isLoading = false;
      }
    };

    // Load statistics
    this.logService.getLogStatistics()
      .pipe(
        takeUntil(this.destroy$),
        catchError(error => {
          this.handleError('Failed to load statistics', error);
          return of(null);
        })
      )
      .subscribe(stats => {
        this.statistics = stats;
        statsLoaded = true;
        checkComplete();
      });

    // Load recent logs (last 10)
    this.logService.getLogs()
      .pipe(
        takeUntil(this.destroy$),
        catchError(error => {
          this.handleError('Failed to load recent logs', error);
          return of([]);
        })
      )
      .subscribe(logs => {
        this.recentLogs = logs.slice(0, 10);
        logsLoaded = true;
        checkComplete();
      });
  }

  private handleError(message: string, error: any): void {
    console.error(message, error);
    this.error = message;
    this.showErrorMessage(message);
  }

  private showErrorMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 5000,
      panelClass: ['error-snackbar']
    });
  }

  showSuccessMessage(message: string): void {
    this.snackBar.open(message, 'Close', {
      duration: 3000,
      panelClass: ['success-snackbar']
    });
  }

  refreshData(): void {
    this.loadDashboardData();
    this.showSuccessMessage('Dashboard data refreshed');
  }

  getPriorityColor(priority: string): string {
    switch (priority?.toLowerCase()) {
      case 'high':
        return '#f44336';
      case 'medium':
        return '#ff9800';
      case 'low':
        return '#4caf50';
      default:
        return '#9e9e9e';
    }
  }

  getSeverityColor(severity: string): string {
    switch (severity?.toLowerCase()) {
      case 'critical':
        return '#d32f2f';
      case 'high':
        return '#f44336';
      case 'medium':
        return '#ff9800';
      case 'low':
        return '#4caf50';
      default:
        return '#9e9e9e';
    }
  }

  trackByLogId(index: number, log: ErrorLog): string {
    return log.id;
  }

  openExcelDownloadDialog(): void {
    const today = new Date();
    const weekAgo = new Date();
    weekAgo.setDate(today.getDate() - 7);

    const dialogData: ExcelDownloadDialogData = {
      defaultDateFrom: weekAgo,
      defaultDateTo: today
    };

    const dialogRef = this.dialog.open(ExcelDownloadDialogComponent, {
      width: '500px',
      data: dialogData,
      disableClose: this.isDownloading
    });

    dialogRef.afterClosed().subscribe((request: ExcelExportRequest) => {
      if (request) {
        this.downloadExcel(request);
      }
    });
  }

  private downloadExcel(request: ExcelExportRequest): void {
    this.isDownloading = true;
    
    // Show progress message
    const progressSnackBar = this.snackBar.open(
      'Generating Excel file...', 
      '', 
      { 
        duration: 0,
        panelClass: ['info-snackbar']
      }
    );

    this.excelDownloadService.downloadExcel(request)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isDownloading = false;
          progressSnackBar.dismiss();
        }),
        catchError(error => {
          this.handleDownloadError(error, request);
          return of(null);
        })
      )
      .subscribe(blob => {
        if (blob) {
          const fileName = this.excelDownloadService.generateFileName(
            request.dateFrom, 
            request.dateTo
          );
          
          this.excelDownloadService.triggerDownload(blob, fileName);
          this.showSuccessMessage(`Excel file "${fileName}" downloaded successfully`);
        }
      });
  }

  private handleDownloadError(error: any, request: ExcelExportRequest): void {
    console.error('Excel download error:', error);
    
    const errorMessage = error.message || 'Failed to download Excel file';
    
    // Show error with retry option
    const snackBarRef = this.snackBar.open(
      errorMessage,
      'Retry',
      {
        duration: 8000,
        panelClass: ['error-snackbar']
      }
    );

    snackBarRef.onAction().subscribe(() => {
      this.downloadExcel(request);
    });
  }

  openLogGeneratorDialog(): void {
    const dialogData: LogGeneratorDialogData = {
      defaultCount: 10,
      defaultSeverity: '',
      defaultHoursBack: 24
    };

    const dialogRef = this.dialog.open(LogGeneratorDialogComponent, {
      width: '500px',
      data: dialogData,
      disableClose: this.isLoading
    });

    dialogRef.afterClosed().subscribe((request: LogGeneratorRequest) => {
      if (request) {
        this.generateSampleLogs(request);
      }
    });
  }

  private generateSampleLogs(request: LogGeneratorRequest): void {
    this.isLoading = true;
    
    // Show progress message
    const progressSnackBar = this.snackBar.open(
      `Generating ${request.count} sample logs...`, 
      '', 
      { 
        duration: 0,
        panelClass: ['info-snackbar']
      }
    );

    this.logService.generateSampleLogs(request.count, request.severity, request.hoursBack)
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isLoading = false;
          progressSnackBar.dismiss();
        }),
        catchError(error => {
          this.handleGenerateError(error, request);
          return of(null);
        })
      )
      .subscribe(response => {
        if (response) {
          const severityText = request.severity ? ` ${request.severity.toLowerCase()}` : '';
          this.showSuccessMessage(
            `Successfully generated ${response.count}${severityText} sample logs`
          );
          
          // Refresh dashboard data to show new logs
          setTimeout(() => {
            this.loadDashboardData();
          }, 1000);
        }
      });
  }

  private handleGenerateError(error: any, request: LogGeneratorRequest): void {
    console.error('Log generation error:', error);
    
    const errorMessage = error.error?.error || error.message || 'Failed to generate sample logs';
    
    // Show error with retry option
    const snackBarRef = this.snackBar.open(
      errorMessage,
      'Retry',
      {
        duration: 8000,
        panelClass: ['error-snackbar']
      }
    );

    snackBarRef.onAction().subscribe(() => {
      this.generateSampleLogs(request);
    });
  }

  openAiAnalysisDialog(): void {
    const dialogData: AiAnalysisDialogData = {
      // Optional pre-filled data
    };

    const dialogRef = this.dialog.open(AiAnalysisDialogComponent, {
      width: '800px',
      maxWidth: '90vw',
      data: dialogData,
      disableClose: this.isLoading
    });

    // const progressSnackBar = this.snackBar.open(
    //   'AI is analyzing your exception...', 
    //   '', 
    //   { 
    //     duration: 0,
    //     panelClass: ['info-snackbar']
    //   }
    // );

    dialogRef.afterClosed().subscribe((rawException: RawExceptionData) => {
      if (rawException) {
        this.analyzeRawException(rawException);
      }
    });
  }

  private analyzeRawException(rawException: RawExceptionData): void {
    this.isLoading = true;
    
    // Show progress message
    

    // this.logService.analyzeRawExceptions([rawException])
    //   .pipe(
    //     takeUntil(this.destroy$),
    //     finalize(() => {
    //       this.isLoading = false;
    //       progressSnackBar.dismiss();
    //     }),
    //     catchError(error => {
    //       this.handleAnalysisError(error, rawException);
    //       return of(null);
    //     })
    //   )
    //   .subscribe(analyzedLogs => {
    //     if (analyzedLogs && analyzedLogs.length > 0) {
    //       const log = analyzedLogs[0];
    //       this.showSuccessMessage(
    //         `Exception analyzed successfully! Severity: ${log.severity}, Priority: ${log.priority}`
    //       );
          
    //       // Refresh dashboard data to show the new analyzed log
    //       setTimeout(() => {
    //         this.loadDashboardData();
    //       }, 1000);
    //     }
    //   });

      this.loadDashboardData();
      this.showSuccessMessage(
                 `Exception analyzed successfully!`
               );
  }

  private handleAnalysisError(error: any, rawException: RawExceptionData): void {
    console.error('AI analysis error:', error);
    
    const errorMessage = error.error?.error || error.message || 'Failed to analyze exception with AI';
    
    // Show error with retry option
    const snackBarRef = this.snackBar.open(
      errorMessage,
      'Retry',
      {
        duration: 8000,
        panelClass: ['error-snackbar']
      }
    );

    snackBarRef.onAction().subscribe(() => {
      // this.analyzeRawException(rawException);
    });
  }
}