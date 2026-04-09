export type ScanType = 'Manual' | 'Lidar' | 'Automatic' | 'Photo';
export type ScanStatus = 'Pending' | 'Processing' | 'Completed' | 'Failed';

export interface ScanSessionDto {
  id: string;
  containerId: string;
  scanType: ScanType;
  itemsDetected: number;
  itemsAdded: number;
  itemsRemoved: number;
  status: ScanStatus;
  errorMessage?: string;
  scannedAt: string;
}
