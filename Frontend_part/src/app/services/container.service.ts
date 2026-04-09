import {Injectable, inject} from '@angular/core';
import {HttpClient} from '@angular/common/http';
import {environment} from '../../environments/environment';
import {ContainerDto, CreateContainerDto, UpdateContainerDto} from '../models/container.model';

@Injectable({providedIn: 'root'})
export class ContainerService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/api/containers`;

  getAll() {
    return this.http.get<ContainerDto[]>(this.baseUrl);
  }

  getById(id: string) {
    return this.http.get<ContainerDto>(`${this.baseUrl}/${id}`);
  }

  create(dto: CreateContainerDto) {
    return this.http.post<ContainerDto>(this.baseUrl, dto);
  }

  update(id: string, dto: UpdateContainerDto) {
    return this.http.put<ContainerDto>(`${this.baseUrl}/${id}`, dto);
  }

  delete(id: string) {
    return this.http.delete(`${this.baseUrl}/${id}`);
  }

  getByNfc(nfcId: string) {
    return this.http.get<ContainerDto>(`${this.baseUrl}/nfc/${nfcId}`);
  }
}
