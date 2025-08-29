import { Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private defaultConfig: MatSnackBarConfig = {
    duration: 5000,
    horizontalPosition: 'right',
    verticalPosition: 'top'
  };

  constructor(private snackBar: MatSnackBar) {}

  showSuccess(message: string, action?: string): void {
    this.snackBar.open(message, action || 'Close', {
      ...this.defaultConfig,
      panelClass: ['success-snackbar']
    });
  }

  showError(message: string, action?: string): void {
    this.snackBar.open(message, action || 'Close', {
      ...this.defaultConfig,
      duration: 8000, // Longer duration for errors
      panelClass: ['error-snackbar']
    });
  }

  showWarning(message: string, action?: string): void {
    this.snackBar.open(message, action || 'Close', {
      ...this.defaultConfig,
      panelClass: ['warning-snackbar']
    });
  }

  showInfo(message: string, action?: string): void {
    this.snackBar.open(message, action || 'Close', {
      ...this.defaultConfig,
      panelClass: ['info-snackbar']
    });
  }

  dismiss(): void {
    this.snackBar.dismiss();
  }
}