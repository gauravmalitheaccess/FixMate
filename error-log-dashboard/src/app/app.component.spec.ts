import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { AppComponent } from './app.component';
import { LogService } from './services/log.service';
import { of } from 'rxjs';

describe('AppComponent', () => {
  let mockLogService: jasmine.SpyObj<LogService>;

  beforeEach(async () => {
    const logServiceSpy = jasmine.createSpyObj('LogService', ['getLogStatistics', 'getLogs']);
    
    await TestBed.configureTestingModule({
      imports: [AppComponent, NoopAnimationsModule],
      providers: [
        provideHttpClient(),
        { provide: LogService, useValue: logServiceSpy }
      ]
    }).compileComponents();

    mockLogService = TestBed.inject(LogService) as jasmine.SpyObj<LogService>;
    mockLogService.getLogStatistics.and.returnValue(of({
      totalLogs: 0,
      criticalCount: 0,
      highCount: 0,
      mediumCount: 0,
      lowCount: 0,
      analyzedCount: 0,
      unanalyzedCount: 0,
      todayCount: 0,
      weekCount: 0,
      monthCount: 0
    }));
    mockLogService.getLogs.and.returnValue(of([]));
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it(`should have the 'error-log-dashboard' title`, () => {
    const fixture = TestBed.createComponent(AppComponent);
    const app = fixture.componentInstance;
    expect(app.title).toEqual('error-log-dashboard');
  });

  it('should render dashboard component', () => {
    const fixture = TestBed.createComponent(AppComponent);
    fixture.detectChanges();
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('app-dashboard')).toBeTruthy();
  });
});
