import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError } from 'rxjs';

import { DashboardComponent } from './dashboard.component';
import { LogService } from '../services/log.service';
import { ErrorLog, LogStatistics } from '../models/error-log.model';

describe('DashboardComponent', () => {
  let component: DashboardComponent;
  let fixture: ComponentFixture<DashboardComponent>;
  let mockLogService: jasmine.SpyObj<LogService>;
  let mockSnackBar: jasmine.SpyObj<MatSnackBar>;

  const mockStatistics: LogStatistics = {
    totalLogs: 100,
    criticalCount: 5,
    highCount: 15,
    mediumCount: 30,
    lowCount: 50,
    analyzedCount: 80,
    unanalyzedCount: 20,
    todayCount: 10,
    weekCount: 60,
    monthCount: 100
  };

  const mockLogs: ErrorLog[] = [
    {
      id: '1',
      timestamp: new Date('2024-01-01T10:00:00Z'),
      source: 'TestApp',
      message: 'Test error message',
      stackTrace: 'Stack trace here',
      severity: 'High',
      priority: 'High',
      aiReasoning: 'AI analysis result',
      analyzedAt: new Date('2024-01-01T11:00:00Z'),
      isAnalyzed: true
    },
    {
      id: '2',
      timestamp: new Date('2024-01-01T09:00:00Z'),
      source: 'TestApp2',
      message: 'Another test error',
      stackTrace: 'Another stack trace',
      severity: 'Medium',
      priority: 'Medium',
      aiReasoning: '',
      isAnalyzed: false
    }
  ];

  beforeEach(async () => {
    const logServiceSpy = jasmine.createSpyObj('LogService', ['getLogStatistics', 'getLogs']);
    const snackBarSpy = jasmine.createSpyObj('MatSnackBar', ['open']);

    await TestBed.configureTestingModule({
      imports: [
        DashboardComponent,
        NoopAnimationsModule
      ],
      providers: [
        { provide: LogService, useValue: logServiceSpy },
        { provide: MatSnackBar, useValue: snackBarSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(DashboardComponent);
    component = fixture.componentInstance;
    mockLogService = TestBed.inject(LogService) as jasmine.SpyObj<LogService>;
    mockSnackBar = TestBed.inject(MatSnackBar) as jasmine.SpyObj<MatSnackBar>;
    
    // Set up default return values to prevent ngOnInit from failing
    mockLogService.getLogStatistics.and.returnValue(of(mockStatistics));
    mockLogService.getLogs.and.returnValue(of(mockLogs));
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should load dashboard data on initialization', (done) => {
      mockLogService.getLogStatistics.and.returnValue(of(mockStatistics));
      mockLogService.getLogs.and.returnValue(of(mockLogs));

      component.ngOnInit();

      setTimeout(() => {
        expect(mockLogService.getLogStatistics).toHaveBeenCalled();
        expect(mockLogService.getLogs).toHaveBeenCalled();
        expect(component.statistics).toEqual(mockStatistics);
        expect(component.recentLogs).toEqual(mockLogs);
        expect(component.isLoading).toBeFalse();
        done();
      });
    });

    it('should handle statistics loading error', (done) => {
      const errorMessage = 'Failed to load statistics';
      mockLogService.getLogStatistics.and.returnValue(throwError(() => new Error('API Error')));
      mockLogService.getLogs.and.returnValue(of(mockLogs));

      component.ngOnInit();

      setTimeout(() => {
        expect(component.error).toBe(errorMessage);
        expect(mockSnackBar.open).toHaveBeenCalledWith(errorMessage, 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
        done();
      });
    });

    it('should handle logs loading error', (done) => {
      const errorMessage = 'Failed to load recent logs';
      mockLogService.getLogStatistics.and.returnValue(of(mockStatistics));
      mockLogService.getLogs.and.returnValue(throwError(() => new Error('API Error')));

      component.ngOnInit();

      setTimeout(() => {
        expect(component.error).toBe(errorMessage);
        expect(mockSnackBar.open).toHaveBeenCalledWith(errorMessage, 'Close', {
          duration: 5000,
          panelClass: ['error-snackbar']
        });
        done();
      });
    });

    it('should limit recent logs to 10 items', (done) => {
      const manyLogs = Array.from({ length: 15 }, (_, i) => ({
        ...mockLogs[0],
        id: `log-${i}`
      }));
      
      mockLogService.getLogStatistics.and.returnValue(of(mockStatistics));
      mockLogService.getLogs.and.returnValue(of(manyLogs));

      component.ngOnInit();

      setTimeout(() => {
        expect(component.recentLogs.length).toBe(10);
        done();
      });
    });
  });

  describe('refreshData', () => {
    it('should reload dashboard data and show success message', (done) => {
      mockLogService.getLogStatistics.and.returnValue(of(mockStatistics));
      mockLogService.getLogs.and.returnValue(of(mockLogs));

      component.refreshData();

      setTimeout(() => {
        expect(mockLogService.getLogStatistics).toHaveBeenCalled();
        expect(mockLogService.getLogs).toHaveBeenCalled();
        expect(mockSnackBar.open).toHaveBeenCalledWith('Dashboard data refreshed', 'Close', {
          duration: 3000,
          panelClass: ['success-snackbar']
        });
        done();
      });
    });
  });

  describe('getPriorityColor', () => {
    it('should return correct colors for priority levels', () => {
      expect(component.getPriorityColor('High')).toBe('#f44336');
      expect(component.getPriorityColor('high')).toBe('#f44336');
      expect(component.getPriorityColor('Medium')).toBe('#ff9800');
      expect(component.getPriorityColor('medium')).toBe('#ff9800');
      expect(component.getPriorityColor('Low')).toBe('#4caf50');
      expect(component.getPriorityColor('low')).toBe('#4caf50');
      expect(component.getPriorityColor('Unknown')).toBe('#9e9e9e');
      expect(component.getPriorityColor('')).toBe('#9e9e9e');
    });
  });

  describe('getSeverityColor', () => {
    it('should return correct colors for severity levels', () => {
      expect(component.getSeverityColor('Critical')).toBe('#d32f2f');
      expect(component.getSeverityColor('critical')).toBe('#d32f2f');
      expect(component.getSeverityColor('High')).toBe('#f44336');
      expect(component.getSeverityColor('high')).toBe('#f44336');
      expect(component.getSeverityColor('Medium')).toBe('#ff9800');
      expect(component.getSeverityColor('medium')).toBe('#ff9800');
      expect(component.getSeverityColor('Low')).toBe('#4caf50');
      expect(component.getSeverityColor('low')).toBe('#4caf50');
      expect(component.getSeverityColor('Unknown')).toBe('#9e9e9e');
      expect(component.getSeverityColor('')).toBe('#9e9e9e');
    });
  });

  describe('trackByLogId', () => {
    it('should return log id for tracking', () => {
      const log = mockLogs[0];
      const result = component.trackByLogId(0, log);
      expect(result).toBe(log.id);
    });
  });

  describe('showSuccessMessage', () => {
    it('should display success message', () => {
      const message = 'Test success message';
      
      component.showSuccessMessage(message);

      expect(mockSnackBar.open).toHaveBeenCalledWith(message, 'Close', {
        duration: 3000,
        panelClass: ['success-snackbar']
      });
    });
  });

  describe('loading states', () => {
    it('should show loading state initially', () => {
      // Use delayed observables to test loading state
      mockLogService.getLogStatistics.and.returnValue(of(mockStatistics).pipe());
      mockLogService.getLogs.and.returnValue(of(mockLogs).pipe());

      component.loadDashboardData();

      expect(component.isLoading).toBeTrue();
    });

    it('should hide loading state after data loads', (done) => {
      mockLogService.getLogStatistics.and.returnValue(of(mockStatistics));
      mockLogService.getLogs.and.returnValue(of(mockLogs));

      component.loadDashboardData();

      setTimeout(() => {
        expect(component.isLoading).toBeFalse();
        done();
      });
    });
  });

  describe('error handling', () => {
    it('should clear error state when loading new data', () => {
      component.error = 'Previous error';
      mockLogService.getLogStatistics.and.returnValue(of(mockStatistics));
      mockLogService.getLogs.and.returnValue(of(mockLogs));

      component.loadDashboardData();

      expect(component.error).toBeNull();
    });
  });

  describe('component lifecycle', () => {
    it('should complete destroy subject on ngOnDestroy', () => {
      spyOn(component['destroy$'], 'next');
      spyOn(component['destroy$'], 'complete');

      component.ngOnDestroy();

      expect(component['destroy$'].next).toHaveBeenCalled();
      expect(component['destroy$'].complete).toHaveBeenCalled();
    });
  });
});