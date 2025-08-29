import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, retry, throwError, timer } from 'rxjs';
import { NotificationService } from '../services/notification.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);

  return next(req).pipe(
    retry({
      count: 3,
      delay: (error, retryCount) => {
        // Only retry on transient errors
        if (isTransientError(error)) {
          const delay = Math.pow(2, retryCount) * 1000; // Exponential backoff
          console.log(`Retrying request (attempt ${retryCount + 1}) in ${delay}ms`);
          return timer(delay);
        }
        // Don't retry for non-transient errors
        throw error;
      }
    }),
    catchError((error: HttpErrorResponse) => {
      handleError(error, notificationService);
      return throwError(() => error);
    })
  );
};

function isTransientError(error: HttpErrorResponse): boolean {
  // Retry on network errors, timeouts, and 5xx server errors
  return !error.status || // Network error
         error.status === 0 || // Network error
         error.status === 408 || // Request Timeout
         error.status === 429 || // Too Many Requests
         (error.status >= 500 && error.status < 600); // Server errors
}

function handleError(error: HttpErrorResponse, notificationService: NotificationService): void {
  let userMessage = 'An unexpected error occurred';

  if (error.error?.message) {
    userMessage = error.error.message;
  } else {
    switch (error.status) {
      case 0:
        userMessage = 'Unable to connect to the server. Please check your internet connection.';
        break;
      case 400:
        userMessage = 'Invalid request. Please check your input and try again.';
        break;
      case 401:
        userMessage = 'You are not authorized to perform this action.';
        break;
      case 403:
        userMessage = 'Access denied. You do not have permission to access this resource.';
        break;
      case 404:
        userMessage = 'The requested resource was not found.';
        break;
      case 408:
        userMessage = 'Request timed out. Please try again.';
        break;
      case 429:
        userMessage = 'Too many requests. Please wait a moment and try again.';
        break;
      case 500:
        userMessage = 'Internal server error. Please try again later.';
        break;
      case 502:
        userMessage = 'Bad gateway. The server is temporarily unavailable.';
        break;
      case 503:
        userMessage = 'Service unavailable. Please try again later.';
        break;
      case 504:
        userMessage = 'Gateway timeout. The server took too long to respond.';
        break;
      default:
        if (error.status >= 500) {
          userMessage = 'Server error. Please try again later.';
        }
        break;
    }
  }

  console.error('HTTP Error:', {
    status: error.status,
    message: error.message,
    url: error.url,
    traceId: error.error?.traceId
  });

  notificationService.showError(userMessage);
}