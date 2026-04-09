import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {environment} from '../../environments/environment';
import {ScanSessionDto, ScanType} from '../models/scan.model';

@Injectable({providedIn: 'root'})
export class ScanService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/scans`;

  upload(file: File, containerId: string, scanType: ScanType) {
    const formData = new FormData();
    formData.append('file', file);
    formData.append('containerId', containerId);
    formData.append('scanType', scanType);

    return this.http.post<ScanSessionDto>(`${this.baseUrl}/upload`, formData);
  }

  getHistory(containerId: string) {
    return this.http.get<ScanSessionDto[]>(this.baseUrl, {params: {containerId}});
  }

  getById(id: string) {
    return this.http.get<ScanSessionDto>(`${this.baseUrl}/${id}`);
  }
}
