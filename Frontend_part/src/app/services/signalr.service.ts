import {Injectable, signal} from '@angular/core';
import * as signalR from '@microsoft/signalr';
import {environment} from '../../environments/environment';
import {ItemAddedEvent, ItemProgressEvent} from '../models/signalr-events.model';

@Injectable({providedIn: 'root'})
export class SignalRService {
  private connection: signalR.HubConnection | null = null;

  readonly scanProgress = signal<{scanId: string; containerId: string; percent: number; stage: string} | null>(null);
  readonly itemAdded = signal<ItemAddedEvent | null>(null);
  readonly scanCompleted = signal<{scanId: string; containerId: string; detected: number; added: number; removed: number} | null>(null);
  readonly scanFailed = signal<{scanId: string; errorMessage: string} | null>(null);
  readonly itemProgress = signal<ItemProgressEvent | null>(null);
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

    this.connection.on('ItemRemoved', (_itemId: string, _containerId: string) => {});

    this.connection.on('ItemProgress', (scanId: string, itemName: string, index: number, total: number, percent: number, stage: string) => {
      this.itemProgress.set({scanId, itemName, index, total, percent, stage});
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
    try {
      if (this.connection?.state === signalR.HubConnectionState.Connected) {
        await this.connection.invoke('JoinContainer', containerId);
        console.log('Joined container:', containerId);
      }
    } catch (e) { console.warn('joinContainer failed:', e); }
  }

  async leaveContainer(containerId: string): Promise<void> {
    try {
      if (this.connection?.state === signalR.HubConnectionState.Connected) {
        await this.connection.invoke('LeaveContainer', containerId);
      }
    } catch (e) { /* ignore on disconnect */ }
  }

  async disconnect(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
      this.isConnected.set(false);
    }
  }
}
