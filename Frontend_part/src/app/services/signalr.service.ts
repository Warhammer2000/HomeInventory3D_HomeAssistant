import {Injectable, signal} from '@angular/core';
import * as signalR from '@microsoft/signalr';
import {environment} from '../../environments/environment';
import {ItemAddedEvent} from '../models/signalr-events.model';

@Injectable({providedIn: 'root'})
export class SignalRService {
  private connection: signalR.HubConnection | null = null;

  readonly scanProgress = signal<{scanId: string; containerId: string; percent: number; stage: string} | null>(null);
  readonly itemAdded = signal<ItemAddedEvent | null>(null);
  readonly scanCompleted = signal<{scanId: string; containerId: string; detected: number; added: number; removed: number} | null>(null);
  readonly scanFailed = signal<{scanId: string; errorMessage: string} | null>(null);
  readonly isConnected = signal(false);

  async connect(): Promise<void> {
    if (this.connection) return;

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(environment.hubUrl)
      .withAutomaticReconnect()
      .build();

    this.connection.on('ScanProgress', (scanId: string, containerId: string, progressPercent: number, stage: string) => {
      this.scanProgress.set({scanId, containerId, percent: progressPercent, stage});
    });

    this.connection.on('ItemAdded', (item: ItemAddedEvent) => {
      this.itemAdded.set(item);
    });

    this.connection.on('ScanCompleted', (scanId: string, containerId: string, itemsDetected: number, itemsAdded: number, itemsRemoved: number) => {
      this.scanCompleted.set({scanId, containerId, detected: itemsDetected, added: itemsAdded, removed: itemsRemoved});
    });

    this.connection.on('ScanFailed', (scanId: string, errorMessage: string) => {
      this.scanFailed.set({scanId, errorMessage});
    });

    this.connection.on('ItemRemoved', (_itemId: string, _containerId: string) => {
      // handled by container detail component if needed
    });

    this.connection.onreconnected(() => this.isConnected.set(true));
    this.connection.onclose(() => this.isConnected.set(false));

    try {
      await this.connection.start();
      this.isConnected.set(true);
    } catch (err) {
      console.error('SignalR connection failed:', err);
    }
  }

  async joinContainer(containerId: string): Promise<void> {
    if (!this.connection) return;
    await this.connection.invoke('JoinContainer', containerId);
  }

  async leaveContainer(containerId: string): Promise<void> {
    if (!this.connection) return;
    await this.connection.invoke('LeaveContainer', containerId);
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.isConnected.set(false);
    }
  }
}
