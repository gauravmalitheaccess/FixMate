import { TestBed } from '@angular/core/testing';
import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { NoopAnimationsModule } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { errorInterceptor } from './error.interceptor';
import { NotificationService } from '../services/notification.service';

describe('ErrorInterceptor', () => {
  let httpClient: HttpClient;
  let httpTestingController: HttpTestingController;
  let notificationService: jasmine.SpyObj<NotificationService>;

  beforeEach(() => {
    const notificationSpy = jasmine.createSpyObj('NotificationService', ['showError']);

    TestBed.configureTestingModule({
      imports: [
        HttpClientTestingModule,
        MatSnackBarModule,
        NoopAnimationsModule
      ],
      providers: [
        provideHttpClient(withInterceptors([errorInterceptor])),
        { provide: NotificationService, useValue: notificationSpy }
      ]
    });

    httpClient = TestBed.inject(HttpClient);
    httpTestingController = TestBed.inject(HttpTestingController);
    notificationService = TestBed.inject(NotificationService) as jasmine.SpyObj<NotificationService>;
  });

  afterEach(() => {
    httpTestingController.verify();
  });

  it('should handle 400 Bad Request error', () => {
    const testUrl = '/api/test';
    const errorMessage = 'Bad Request';

    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected an error'),
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(400);
        expect(notificationService.showError).toHaveBeenCalledWith(
          'Invalid request. Please check your input and try again.'
        );
      }
    });

    const req = httpTestingController.expectOne(testUrl);
    req.flush(errorMessage, { status: 400, statusText: 'Bad Request' });
  });

  it('should handle 401 Unauthorized error', () => {
    const testUrl = '/api/test';

    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected an error'),
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(401);
        expect(notificationService.showError).toHaveBeenCalledWith(
          'You are not authorized to perform this action.'
        );
      }
    });

    const req = httpTestingController.expectOne(testUrl);
    req.flush('Unauthorized', { status: 401, statusText: 'Unauthorized' });
  });

  it('should handle 404 Not Found error', () => {
    const testUrl = '/api/test';

    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected an error'),
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(404);
        expect(notificationService.showError).toHaveBeenCalledWith(
          'The requested resource was not found.'
        );
      }
    });

    const req = httpTestingController.expectOne(testUrl);
    req.flush('Not Found', { status: 404, statusText: 'Not Found' });
  });

  it('should handle 500 Internal Server Error', () => {
    const testUrl = '/api/test';

    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected an error'),
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(500);
        expect(notificationService.showError).toHaveBeenCalledWith(
          'Internal server error. Please try again later.'
        );
      }
    });

    const req = httpTestingController.expectOne(testUrl);
    req.flush('Internal Server Error', { status: 500, statusText: 'Internal Server Error' });
  });

  it('should handle network error (status 0)', () => {
    const testUrl = '/api/test';

    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected an error'),
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(0);
        expect(notificationService.showError).toHaveBeenCalledWith(
          'Unable to connect to the server. Please check your internet connection.'
        );
      }
    });

    const req = httpTestingController.expectOne(testUrl);
    req.error(new ProgressEvent('error'), { status: 0 });
  });

  it('should use custom error message from server response', () => {
    const testUrl = '/api/test';
    const customMessage = 'Custom error message from server';

    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected an error'),
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(400);
        expect(notificationService.showError).toHaveBeenCalledWith(customMessage);
      }
    });

    const req = httpTestingController.expectOne(testUrl);
    req.flush({ message: customMessage }, { status: 400, statusText: 'Bad Request' });
  });

  it('should handle timeout error (408)', () => {
    const testUrl = '/api/test';

    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected an error'),
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(408);
        expect(notificationService.showError).toHaveBeenCalledWith(
          'Request timed out. Please try again.'
        );
      }
    });

    const req = httpTestingController.expectOne(testUrl);
    req.flush('Request Timeout', { status: 408, statusText: 'Request Timeout' });
  });

  it('should handle too many requests error (429)', () => {
    const testUrl = '/api/test';

    httpClient.get(testUrl).subscribe({
      next: () => fail('Expected an error'),
      error: (error: HttpErrorResponse) => {
        expect(error.status).toBe(429);
        expect(notificationService.showError).toHaveBeenCalledWith(
          'Too many requests. Please wait a moment and try again.'
        );
      }
    });

    const req = httpTestingController.expectOne(testUrl);
    req.flush('Too Many Requests', { status: 429, statusText: 'Too Many Requests' });
  });
});