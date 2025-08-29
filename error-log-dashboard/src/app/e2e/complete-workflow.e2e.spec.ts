import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';

import { DashboardComponent } from '../dashboard/dashboard.component';
import { LogsComponent } from '../pages/logs/logs.component';
import { LogService } from '../services/log.service';
import { ExcelDownloadService } from '../services/excel-download.service';
import { ErrorLog, LogStatistics, LogFilter } from '../models/error-log.model';
import { ExcelExportRequest } from '../models/excel-export.model';

describe('Complete User Workflow E2E Tests', () => {
  let httpMock: HttpTestingController;
  let mockDialog: jasmine.SpyObj<MatDialog>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;
  let mockRouter: jasmine.SpyObj<Router>;

  const mockLogs: ErrorLog[] = [
    {
      id: '1',
      timestamp: new Date('2024-01-15T10:00:00Z'),
      source: 'WebApp.Controllers.AuthController',
      message: 'User authentication failed',
      stackTrace: 'at AuthController.Login() line 45',
      severity: 'High',
      priority: 'High',
      aiReasoning: 'Authentication failures can lead to security breaches',
      analyzedAt: new Date('2024-01-15T10:05:00Z'),
      isAnalyzed: true
    },
    {
      id: '2',
      timestamp: new Date('2024-01-15T11:00:00Z'),
      source: 'WebApp.Services.DatabaseService',
      message: 'Database connection timeout',
      stackTrace: 'at DatabaseService.Connect() line 23',
      severity: 'Critical',
      priority: 'High',
      aiReasoning: 'Database connectivity affects core application functionality',
      analyzedAt: new Date('2024-01-15T11:05:00Z'),
      isAnalyzed: true
    },
    {
      id: '3',
      timestamp: new Date('2024-01-15T12:00:00Z'),
      source: 'WebApp.Utils.ConfigHelper',
      message: 'Configuration file not found',
      stackTrace: 'at ConfigHelper.LoadConfig() line 12',
      severity: 'Medium',
      priority: 'Low',
      aiReasoning: 'Configuration issues have fallback mechanisms',
      analyzedAt: new Date('2024-01-15T12:05:00Z'),
      isAnalyzed: true
    }
  ];

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
    mockDialog = jasmine.createSpyObj('MatDialog', ['open']);
    mockSnackBar = jasmine.createSpyObj('MatSnackBar', ['open']);
    mockRouter = jasmine.createSpyObj('Router', ['navigate']);

    await TestBed.configureTestingModule({
      imports: [
        DashboardComponent,
        LogsComponent,
        NoopAnimationsModule,
        HttpClientTestingModule
      ],
      providers: [
        LogService,
        ExcelDownloadService,
        { provide: MatDialog, useValue: mockDialog },
        { provide: MatSnackBar, useValue: mockSnackBar },
        { provide: Router, useValue: mockRouter }
      ]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('Dashboard to Logs Navigation Workflow', () => {
    it('should navigate from dashboard to logs page and maintain context', async () => {
      // Arrange
      const dashboardFixture = TestBed.createComponent(DashboardComponent);
      const dashboardComponent = dashboardFixture.componentInstance;

      // Act - Load dashboard
      dashboardFixture.detectChanges();

      // Expect statistics request
      const statsReq = httpMock.expectOne('/api/log/stats');
      expect(statsReq.request.method).toBe('GET');
      statsReq.flush(mockStatistics);

      // Expect initial logs request
      const logsReq = httpMock.expectOne(req => req.url === '/api/log' && req.method === 'GET');
      logsReq.flush(mockLogs);

      dashboardFixture.detectChanges();

      // Verify dashboard loaded correctly
      expect(dashboardComponent.statistics).toEqual(mockStatistics);
      expect(dashboardComponent.recentLogs.length).toBe(3);

      // Act - Navigate to logs page
      const viewAllButton = dashboardFixture.debugElement.nativeElement.querySelector(
        'button[routerLink="/logs"]'
      );
      expect(viewAllButton).toBeTruthy();

      // Simulate navigation
      mockRouter.navigate.and.returnValue(Promise.resolve(true));
      viewAllButton.click();

      // Assert
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/logs']);
    });
  });

  describe('Complete Log Filtering Workflow', () => {
    it('should filter logs by severity and priority with real-time updates', async () => {
      // Arrange
      const logsFixture = TestBed.createComponent(LogsComponent);
      const logsComponent = logsFixture.componentInstance;

      // Act - Load logs page
      logsFixture.detectChanges();

      // Expect initial logs request
      const initialReq = httpMock.expectOne(req => req.url === '/api/log' && req.method === 'GET');
      initialReq.flush(mockLogs);

      logsFixture.detectChanges();

      // Verify initial load
      expect(logsComponent.logs.length).toBe(3);

      // Act - Apply severity filter
      const severityFilter = logsFixture.debugElement.nativeElement.querySelector(
        'mat-select[formControlName="severity"]'
      );
      expect(severityFilter).toBeTruthy();

      // Simulate filter change
      const filter: LogFilter = { severity: 'High' };
      logsComponent.onFilterChange(filter);

      // Expect filtered request
      const filteredReq = httpMock.expectOne(req => 
        req.url === '/api/log' && 
        req.params.get('severity') === 'High'
      );
      
      const filteredLogs = mockLogs.filter(log => log.severity === 'High');
      filteredReq.flush(filteredLogs);

      logsFixture.detectChanges();

      // Assert
      expect(logsComponent.logs.length).toBe(1);
      expect(logsComponent.logs[0].severity).toBe('High');
    });

    it('should handle date range filtering with validation', async () => {
      // Arrange
      const logsFixture = TestBed.createComponent(LogsComponent);
      const logsComponent = logsFixture.componentInstance;

      logsFixture.detectChanges();

      // Initial request
      const initialReq = httpMock.expectOne('/api/log');
      initialReq.flush(mockLogs);

      // Act - Apply date range filter
      const dateFrom = new Date('2024-01-15');
      const dateTo = new Date('2024-01-15');

      const dateFilter: LogFilter = {
        dateFrom: dateFrom,
        dateTo: dateTo
      };
      logsComponent.onFilterChange(dateFilter);

      // Expect date-filtered request
      const dateFilteredReq = httpMock.expectOne(req => 
        req.url === '/api/log' && 
        req.params.get('dateFrom') === '2024-01-15' &&
        req.params.get('dateTo') === '2024-01-15'
      );
      
      dateFilteredReq.flush(mockLogs);

      // Assert
      expect(logsComponent.currentFilter.dateFrom).toEqual(dateFrom);
      expect(logsComponent.currentFilter.dateTo).toEqual(dateTo);
    });
  });

  describe('Complete Excel Export Workflow', () => {
    it('should complete full Excel export workflow from logs page', async () => {
      // Arrange
      const logsFixture = TestBed.createComponent(LogsComponent);
      const logsComponent = logsFixture.componentInstance;

      const mockExportRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-15'),
        dateTo: new Date('2024-01-15'),
        includeUnanalyzed: false
      };

      const mockBlob = new Blob(['mock excel data'], { 
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' 
      });

      // Setup dialog mock
      const mockDialogRef = {
        afterClosed: () => of(mockExportRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);

      // Setup snackbar mocks
      const mockProgressSnackBar = { dismiss: jasmine.createSpy('dismiss') };
      const mockSuccessSnackBar = { onAction: () => of(null) };
      mockSnackBar.open.and.returnValues(
        mockProgressSnackBar as any,
        mockSuccessSnackBar as any
      );

      // Act - Load page
      logsFixture.detectChanges();

      const initialReq = httpMock.expectOne('/api/log');
      initialReq.flush(mockLogs);

      // Act - Trigger Excel download
      const downloadButton = logsFixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download filtered logs as Excel"]'
      );
      expect(downloadButton).toBeTruthy();

      downloadButton.click();

      // Expect Excel export request
      const exportReq = httpMock.expectOne(req => 
        req.url === '/api/log/export' && 
        req.method === 'GET' &&
        req.params.get('fromDate') === '2024-01-15' &&
        req.params.get('toDate') === '2024-01-15'
      );
      
      exportReq.flush(mockBlob);

      logsFixture.detectChanges();

      // Assert
      expect(mockDialog.open).toHaveBeenCalled();
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Generating Excel file...',
        '',
        jasmine.objectContaining({
          duration: 0,
          panelClass: ['info-snackbar']
        })
      );
      expect(mockProgressSnackBar.dismiss).toHaveBeenCalled();
    });

    it('should handle Excel export errors with retry functionality', async () => {
      // Arrange
      const logsFixture = TestBed.createComponent(LogsComponent);
      const logsComponent = logsFixture.componentInstance;

      const mockExportRequest: ExcelExportRequest = {
        dateFrom: new Date('2024-01-15'),
        dateTo: new Date('2024-01-15')
      };

      const mockDialogRef = {
        afterClosed: () => of(mockExportRequest)
      };
      mockDialog.open.and.returnValue(mockDialogRef as any);

      const mockProgressSnackBar = { dismiss: jasmine.createSpy('dismiss') };
      const mockErrorSnackBar = { onAction: () => of('retry') };
      mockSnackBar.open.and.returnValues(
        mockProgressSnackBar as any,
        mockErrorSnackBar as any,
        mockProgressSnackBar as any
      );

      // Act - Load page
      logsFixture.detectChanges();

      const initialReq = httpMock.expectOne('/api/log');
      initialReq.flush(mockLogs);

      // Act - Trigger Excel download
      const downloadButton = logsFixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download filtered logs as Excel"]'
      );
      downloadButton.click();

      // First request fails
      const failedExportReq = httpMock.expectOne('/api/log/export');
      failedExportReq.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

      // Retry request succeeds
      const retryExportReq = httpMock.expectOne('/api/log/export');
      retryExportReq.flush(new Blob(['retry success']));

      // Assert
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        'Http failure response for /api/log/export: 500 Internal Server Error',
        'Retry',
        jasmine.objectContaining({
          duration: 8000,
          panelClass: ['error-snackbar']
        })
      );
    });
  });

  describe('Real-time Data Updates Workflow', () => {
    it('should handle real-time log updates and refresh display', async () => {
      // Arrange
      const dashboardFixture = TestBed.createComponent(DashboardComponent);
      const dashboardComponent = dashboardFixture.componentInstance;

      // Act - Initial load
      dashboardFixture.detectChanges();

      const statsReq = httpMock.expectOne('/api/log/stats');
      statsReq.flush(mockStatistics);

      const logsReq = httpMock.expectOne('/api/log');
      logsReq.flush(mockLogs);

      dashboardFixture.detectChanges();

      // Verify initial state
      expect(dashboardComponent.statistics?.totalLogs).toBe(150);
      expect(dashboardComponent.recentLogs.length).toBe(3);

      // Act - Simulate refresh (new logs arrived)
      const newLog: ErrorLog = {
        id: '4',
        timestamp: new Date('2024-01-15T13:00:00Z'),
        source: 'WebApp.NewService',
        message: 'New error occurred',
        stackTrace: 'at NewService.Method() line 1',
        severity: 'High',
        priority: 'Medium',
        aiReasoning: 'New error analysis',
        analyzedAt: new Date('2024-01-15T13:05:00Z'),
        isAnalyzed: true
      };

      const updatedLogs = [...mockLogs, newLog];
      const updatedStats: LogStatistics = {
        ...mockStatistics,
        totalLogs: 151,
        highCount: 26
      };

      // Trigger refresh
      dashboardComponent.refreshData();

      // Expect refresh requests
      const refreshStatsReq = httpMock.expectOne('/api/log/stats');
      refreshStatsReq.flush(updatedStats);

      const refreshLogsReq = httpMock.expectOne('/api/log');
      refreshLogsReq.flush(updatedLogs);

      dashboardFixture.detectChanges();

      // Assert
      expect(dashboardComponent.statistics?.totalLogs).toBe(151);
      expect(dashboardComponent.statistics?.highCount).toBe(26);
      expect(dashboardComponent.recentLogs.length).toBe(4);
    });
  });

  describe('Error Handling and Recovery Workflow', () => {
    it('should handle API failures gracefully and allow recovery', async () => {
      // Arrange
      const logsFixture = TestBed.createComponent(LogsComponent);
      const logsComponent = logsFixture.componentInstance;

      const mockErrorSnackBar = { onAction: () => of('retry') };
      mockSnackBar.open.and.returnValue(mockErrorSnackBar as any);

      // Act - Load page with API failure
      logsFixture.detectChanges();

      const failedReq = httpMock.expectOne('/api/log');
      failedReq.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

      logsFixture.detectChanges();

      // Assert error state
      expect(logsComponent.isLoading).toBe(false);
      expect(logsComponent.logs.length).toBe(0);
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        jasmine.stringContaining('500'),
        'Retry',
        jasmine.objectContaining({
          panelClass: ['error-snackbar']
        })
      );

      // Act - Retry after error
      logsComponent.loadLogs();

      const retryReq = httpMock.expectOne('/api/log');
      retryReq.flush(mockLogs);

      logsFixture.detectChanges();

      // Assert recovery
      expect(logsComponent.logs.length).toBe(3);
      expect(logsComponent.isLoading).toBe(false);
    });

    it('should handle network connectivity issues', async () => {
      // Arrange
      const dashboardFixture = TestBed.createComponent(DashboardComponent);
      const dashboardComponent = dashboardFixture.componentInstance;

      const mockErrorSnackBar = { onAction: () => of(null) };
      mockSnackBar.open.and.returnValue(mockErrorSnackBar as any);

      // Act - Simulate network error
      dashboardFixture.detectChanges();

      const networkErrorReq = httpMock.expectOne('/api/log/stats');
      networkErrorReq.error(new ErrorEvent('Network error'), {
        status: 0,
        statusText: 'Unknown Error'
      });

      // Assert
      expect(mockSnackBar.open).toHaveBeenCalledWith(
        jasmine.stringContaining('network'),
        'Retry',
        jasmine.objectContaining({
          panelClass: ['error-snackbar']
        })
      );
    });
  });

  describe('Performance and User Experience', () => {
    it('should show loading states during data operations', async () => {
      // Arrange
      const logsFixture = TestBed.createComponent(LogsComponent);
      const logsComponent = logsFixture.componentInstance;

      // Act - Start loading
      logsFixture.detectChanges();

      // Assert loading state
      expect(logsComponent.isLoading).toBe(true);
      
      const loadingIndicator = logsFixture.debugElement.nativeElement.querySelector(
        'mat-progress-spinner'
      );
      expect(loadingIndicator).toBeTruthy();

      // Complete loading
      const req = httpMock.expectOne('/api/log');
      req.flush(mockLogs);

      logsFixture.detectChanges();

      // Assert loaded state
      expect(logsComponent.isLoading).toBe(false);
      expect(logsFixture.debugElement.nativeElement.querySelector('mat-progress-spinner')).toBeFalsy();
    });

    it('should maintain responsive design across different screen sizes', async () => {
      // Arrange
      const dashboardFixture = TestBed.createComponent(DashboardComponent);

      // Simulate mobile viewport
      Object.defineProperty(window, 'innerWidth', {
        writable: true,
        configurable: true,
        value: 768
      });

      // Act
      dashboardFixture.detectChanges();

      const statsReq = httpMock.expectOne('/api/log/stats');
      statsReq.flush(mockStatistics);

      const logsReq = httpMock.expectOne('/api/log');
      logsReq.flush(mockLogs);

      dashboardFixture.detectChanges();

      // Assert responsive layout
      const container = dashboardFixture.debugElement.nativeElement.querySelector('.dashboard-container');
      expect(container).toBeTruthy();

      const cards = dashboardFixture.debugElement.nativeElement.querySelectorAll('mat-card');
      expect(cards.length).toBeGreaterThan(0);
    });
  });

  describe('Accessibility Compliance', () => {
    it('should have proper ARIA labels and keyboard navigation', async () => {
      // Arrange
      const logsFixture = TestBed.createComponent(LogsComponent);

      // Act
      logsFixture.detectChanges();

      const req = httpMock.expectOne('/api/log');
      req.flush(mockLogs);

      logsFixture.detectChanges();

      // Assert accessibility features
      const downloadButton = logsFixture.debugElement.nativeElement.querySelector(
        'button[matTooltip="Download filtered logs as Excel"]'
      );
      expect(downloadButton).toBeTruthy();
      expect(downloadButton.getAttribute('matTooltip')).toBeTruthy();

      const filterInputs = logsFixture.debugElement.nativeElement.querySelectorAll('input, select');
      filterInputs.forEach((input: HTMLElement) => {
        expect(input.tabIndex).not.toBe(-1);
      });

      const table = logsFixture.debugElement.nativeElement.querySelector('mat-table');
      expect(table).toBeTruthy();
    });
  });
});