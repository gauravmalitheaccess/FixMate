import { TestBed } from '@angular/core/testing';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NotificationService } from './notification.service';

describe('NotificationService', () => {
  let service: NotificationService;
  let snackBarSpy: jasmine.SpyObj<MatSnackBar>;

  beforeEach(() => {
    const spy = jasmine.createSpyObj('MatSnackBar', ['open', 'dismiss']);

    TestBed.configureTestingModule({
      providers: [
        NotificationService,
        { provide: MatSnackBar, useValue: spy }
      ]
    });

    service = TestBed.inject(NotificationService);
    snackBarSpy = TestBed.inject(MatSnackBar) as jasmine.SpyObj<MatSnackBar>;
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  it('should show success notification', () => {
    const message = 'Success message';
    const action = 'OK';

    service.showSuccess(message, action);

    expect(snackBarSpy.open).toHaveBeenCalledWith(message, action, {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: ['success-snackbar']
    });
  });

  it('should show success notification with default action', () => {
    const message = 'Success message';

    service.showSuccess(message);

    expect(snackBarSpy.open).toHaveBeenCalledWith(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: ['success-snackbar']
    });
  });

  it('should show error notification with longer duration', () => {
    const message = 'Error message';
    const action = 'Retry';

    service.showError(message, action);

    expect(snackBarSpy.open).toHaveBeenCalledWith(message, action, {
      duration: 8000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: ['error-snackbar']
    });
  });

  it('should show error notification with default action', () => {
    const message = 'Error message';

    service.showError(message);

    expect(snackBarSpy.open).toHaveBeenCalledWith(message, 'Close', {
      duration: 8000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: ['error-snackbar']
    });
  });

  it('should show warning notification', () => {
    const message = 'Warning message';
    const action = 'Dismiss';

    service.showWarning(message, action);

    expect(snackBarSpy.open).toHaveBeenCalledWith(message, action, {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: ['warning-snackbar']
    });
  });

  it('should show info notification', () => {
    const message = 'Info message';

    service.showInfo(message);

    expect(snackBarSpy.open).toHaveBeenCalledWith(message, 'Close', {
      duration: 5000,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: ['info-snackbar']
    });
  });

  it('should dismiss notifications', () => {
    service.dismiss();

    expect(snackBarSpy.dismiss).toHaveBeenCalled();
  });
});