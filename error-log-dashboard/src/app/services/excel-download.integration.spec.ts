import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { HttpResponse, HttpErrorResponse } from '@angular/common/http';

import { ExcelDownloadService } from './excel-download.service';
import { ExcelExportRequest, ExcelExportResponse } from '../models/excel-export.model';

describe('ExcelDownloadService Integration', () => {
  let service: ExcelDownloadService;
  let httpMock: HttpTestingController;

  const baseUrl = 'https://localhost:44314/api/logs';

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

  describe('downloadExcel', () => {
    it('should download Excel file successfully', (done) => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07'),
        includeUnanalyzed: false
      };

      const mockBlob = new Blob(['mock excel data'], {
        type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet'
      });

      service.downloadExcel(request).subscribe({
        next: (blob) => {
          expect(blob).toEqual(mockBlob);
          expect(blob.type).toBe('application/vnd.openxmlformats-officedocument.spreadsheetml.sheet');
          done();
        },
        error: done.fail
      });

      const req = httpMock.expectOne((request) => {
        return request.url === `${baseUrl}/export` &&
          request.method === 'GET' &&
          request.params.has('dateFrom') &&
          request.params.has('dateTo') &&
          request.params.has('includeUnanalyzed');
      });

      expect(req.request.responseType).toBe('blob');
      expect(req.request.params.get('includeUnanalyzed')).toBe('false');

      req.flush(mockBlob, {
        status: 200,
        statusText: 'OK',
        headers: {
          'Content-Type': 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
          'Content-Disposition': 'attachment; filename="error-logs-2024-01-01.xlsx"'
        }
      });
    });

    it('should handle server errors with retry', (done) => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07'),
        includeUnanalyzed: true
      };

      let attemptCount = 0;

      service.downloadExcel(request).subscribe({
        next: () => done.fail('Should have failed'),
        error: (error) => {
          expect(error.message).toContain('Server returned error code 500');
          expect(attemptCount).toBe(3); // Initial + 2 retries
          done();
        }
      });

      // Handle initial request and 2 retries
      for (let i = 0; i < 3; i++) {
        const req = httpMock.expectOne((request) => {
          return request.url === `${baseUrl}/export` &&
            request.method === 'GET';
        });
        attemptCount++;
        req.flush('Server Error', { status: 500, statusText: 'Internal Server Error' });
      }
    });

    it('should handle network errors', (done) => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      service.downloadExcel(request).subscribe({
        next: () => done.fail('Should have failed'),
        error: (error) => {
          expect(error.message).toContain('Network error');
          done();
        }
      });

      const req = httpMock.expectOne((request) => {
        return request.url === `${baseUrl}/export` &&
          request.method === 'GET';
      });
      req.error(new ProgressEvent('Network error'), { status: 0 });
    });

    it('should handle 404 errors appropriately', (done) => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      service.downloadExcel(request).subscribe({
        next: () => done.fail('Should have failed'),
        error: (error) => {
          expect(error.message).toBe('No data found for the selected date range');
          done();
        }
      });

      const req = httpMock.expectOne((request) => {
        return request.url === `${baseUrl}/export` &&
          request.method === 'GET';
      });
      req.flush('Not Found', { status: 404, statusText: 'Not Found' });
    });
  });

  describe('downloadExcelByDate', () => {
    it('should download Excel file for specific date', (done) => {
      const date = new Date('2024-01-15');
      const mockBlob = new Blob(['mock excel data']);

      service.downloadExcelByDate(date).subscribe({
        next: (blob) => {
          expect(blob).toEqual(mockBlob);
          done();
        },
        error: done.fail
      });

      const expectedUrl = `${baseUrl}/export/2024-01-15`;
      const req = httpMock.expectOne(expectedUrl);
      expect(req.request.method).toBe('GET');
      expect(req.request.responseType).toBe('blob');

      req.flush(mockBlob, { status: 200, statusText: 'OK' });
    });
  });

  describe('getExportInfo', () => {
    it('should get export information', (done) => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      const mockResponse: ExcelExportResponse = {
        fileName: 'error-logs-2024-01-01-to-2024-01-07.xlsx',
        downloadUrl: '/api/logs/export/download/123',
        fileSize: 1024,
        generatedAt: new Date('2024-01-08T10:00:00Z')
      };

      service.getExportInfo(request).subscribe({
        next: (response) => {
          expect(response).toEqual(mockResponse);
          done();
        },
        error: done.fail
      });

      const req = httpMock.expectOne((request) => {
        return request.url === `${baseUrl}/export/info` &&
          request.method === 'GET' &&
          request.params.has('dateFrom') &&
          request.params.has('dateTo');
      });
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    });
  });

  describe('File handling utilities', () => {
    it('should generate correct filename for single date', () => {
      const date = new Date('2024-01-15');
      const filename = service.generateFileName(date, date);
      expect(filename).toBe('error-logs-2024-01-15.xlsx');
    });

    it('should generate correct filename for date range', () => {
      const dateFrom = new Date('2024-01-01');
      const dateTo = new Date('2024-01-07');
      const filename = service.generateFileName(dateFrom, dateTo);
      expect(filename).toBe('error-logs-2024-01-01-to-2024-01-07.xlsx');
    });

    it('should trigger file download', () => {
      const mockBlob = new Blob(['test data']);
      const filename = 'test-file.xlsx';

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

      service.triggerDownload(mockBlob, filename);

      expect(document.createElement).toHaveBeenCalledWith('a');
      expect(window.URL.createObjectURL).toHaveBeenCalledWith(mockBlob);
      expect(mockLink.href).toBe('mock-url');
      expect(mockLink.download).toBe(filename);
      expect(mockLink.style.display).toBe('none');
      expect(document.body.appendChild).toHaveBeenCalledWith(mockLink as any);
      expect(mockLink.click).toHaveBeenCalled();
      expect(document.body.removeChild).toHaveBeenCalledWith(mockLink as any);
      expect(window.URL.revokeObjectURL).toHaveBeenCalledWith('mock-url');
    });
  });

  describe('Error handling scenarios', () => {
    it('should handle empty blob response', (done) => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      service.downloadExcel(request).subscribe({
        next: () => done.fail('Should have failed'),
        error: (error) => {
          expect(error.message).toBe('No file content received');
          done();
        }
      });

      const req = httpMock.expectOne((request) => {
        return request.url === `${baseUrl}/export` &&
          request.method === 'GET';
      });
      req.flush(null, { status: 200, statusText: 'OK' });
    });

    it('should handle connection timeout', (done) => {
      const request: ExcelExportRequest = {
        dateFrom: new Date('2024-01-01'),
        dateTo: new Date('2024-01-07')
      };

      service.downloadExcel(request).subscribe({
        next: () => done.fail('Should have failed'),
        error: (error) => {
          expect(error.message).toContain('Unable to connect to the server');
          done();
        }
      });

      const req = httpMock.expectOne((request) => {
        return request.url === `${baseUrl}/export` &&
          request.method === 'GET';
      });
      req.error(new ProgressEvent('timeout'), { status: 0 });
    });
  });
});