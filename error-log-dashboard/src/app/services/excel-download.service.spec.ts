import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { ExcelDownloadService } from './excel-download.service';
import { ExcelExportRequest } from '../models/excel-export.model';

describe('ExcelDownloadService', () => {
  let service: ExcelDownloadService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ExcelDownloadService]
    });
    service = TestBed.inject(ExcelDownloadService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('downloadExcel', () => {
    it('should download Excel file with date range', () => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-02'),
        includeUnanalyzed: false
      };
      const mockBlob = new Blob(['test'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

      service.downloadExcel(request).subscribe(blob => {
        expect(blob).toEqual(mockBlob);
      });

      const req = httpMock.expectOne(req => 
        req.url === 'https://localhost:44314/api/logs/export' &&
        req.params.get('dateFrom') === request.dateFrom.toISOString() &&
        req.params.get('dateTo') === request.dateTo.toISOString() &&
        req.params.get('includeUnanalyzed') === 'false'
      );
      expect(req.request.method).toBe('GET');
      expect(req.request.responseType).toBe('blob');

      req.flush(mockBlob);
    });

    it('should handle download errors', () => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-02'),
        includeUnanalyzed: false
      };

      service.downloadExcel(request).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          expect(error.message).toContain('Server returned error code 404');
        }
      });

      const req = httpMock.expectOne(req => req.url.includes('/export'));
      req.flush('Not found', { status: 404, statusText: 'Not Found' });
    });

    it('should retry on failure', () => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-02'),
        includeUnanalyzed: false
      };
      const mockBlob = new Blob(['test'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

      service.downloadExcel(request).subscribe(blob => {
        expect(blob).toEqual(mockBlob);
      });

      // First request fails
      const req1 = httpMock.expectOne(req => req.url.includes('/export'));
      req1.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

      // Second request fails
      const req2 = httpMock.expectOne(req => req.url.includes('/export'));
      req2.flush('Server error', { status: 500, statusText: 'Internal Server Error' });

      // Third request succeeds
      const req3 = httpMock.expectOne(req => req.url.includes('/export'));
      req3.flush(mockBlob);
    });
  });

  describe('downloadExcelByDate', () => {
    it('should download Excel file for specific date', () => {
      const date = new Date('2024-01-01');
      const mockBlob = new Blob(['test'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });

      service.downloadExcelByDate(date).subscribe(blob => {
        expect(blob).toEqual(mockBlob);
      });

      const req = httpMock.expectOne('https://localhost:44314/api/logs/export/2024-01-01');
      expect(req.request.method).toBe('GET');
      expect(req.request.responseType).toBe('blob');

      req.flush(mockBlob);
    });
  });

  describe('generateFileName', () => {
    it('should generate filename for single date', () => {
      const date = new Date('2024-01-01');
      const fileName = service.generateFileName(date, date);
      expect(fileName).toBe('error-logs-2024-01-01.xlsx');
    });

    it('should generate filename for date range', () => {
      const dateFrom = new Date('2024-01-01');
      const dateTo = new Date('2024-01-07');
      const fileName = service.generateFileName(dateFrom, dateTo);
      expect(fileName).toBe('error-logs-2024-01-01-to-2024-01-07.xlsx');
    });
  });

  describe('triggerDownload', () => {
    it('should create download link and trigger download', () => {
      const mockBlob = new Blob(['test'], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
      const fileName = 'test-file.xlsx';

      // Mock DOM methods
      const mockLink = {
        href: '',
        download: '',
        style: { display: '' },
        click: jasmine.createSpy('click')
      };
      spyOn(document, 'createElement').and.returnValue(mockLink as any);
      spyOn(document.body, 'appendChild');
      spyOn(document.body, 'removeChild');
      spyOn(window.URL, 'createObjectURL').and.returnValue('mock-url');
      spyOn(window.URL, 'revokeObjectURL');

      service.triggerDownload(mockBlob, fileName);

      expect(document.createElement).toHaveBeenCalledWith('a');
      expect(window.URL.createObjectURL).toHaveBeenCalledWith(mockBlob);
      expect(mockLink.href).toBe('mock-url');
      expect(mockLink.download).toBe(fileName);
      expect(mockLink.style.display).toBe('none');
      expect(document.body.appendChild).toHaveBeenCalledWith(mockLink as any);
      expect(mockLink.click).toHaveBeenCalled();
      expect(document.body.removeChild).toHaveBeenCalledWith(mockLink as any);
      expect(window.URL.revokeObjectURL).toHaveBeenCalledWith('mock-url');
    });
  });

  describe('error handling', () => {
    it('should handle network errors', () => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-02'),
        includeUnanalyzed: false
      };

      service.downloadExcel(request).subscribe({
        next: () => fail('Should have failed'),
        error: (error) => {
          expect(error.message).toContain('Network error');
        }
      });

      const req = httpMock.expectOne(req => req.url.includes('/export'));
      req.error(new ErrorEvent('Network error', { message: 'Connection failed' }));
    });

    it('should handle server errors with appropriate messages', () => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-02'),
        includeUnanalyzed: false
      };

      const testCases = [
        { status: 404, expectedMessage: 'No data found for the selected date range' },
        { status: 500, expectedMessage: 'Server error occurred while generating the Excel file' },
        { status: 0, expectedMessage: 'Unable to connect to the server. Please check your connection.' }
      ];

      testCases.forEach(testCase => {
        service.downloadExcel(request).subscribe({
          next: () => fail('Should have failed'),
          error: (error) => {
            expect(error.message).toBe(testCase.expectedMessage);
          }
        });

        const req = httpMock.expectOne(req => req.url.includes('/export'));
        req.flush('Error', { status: testCase.status, statusText: 'Error' });
      });
    });
  });
});