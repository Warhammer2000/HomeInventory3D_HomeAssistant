export interface ItemAddedEvent {
  id: string;
  containerId: string;
  name: string;
  tags: string[];
  positionX?: number;
  positionY?: number;
  positionZ?: number;
  rotationX?: number;
  rotationY?: number;
  rotationZ?: number;
  bboxMinX?: number;
  bboxMinY?: number;
  bboxMinZ?: number;
  bboxMaxX?: number;
  bboxMaxY?: number;
  bboxMaxZ?: number;
  meshUrl?: string;
  thumbnailUrl?: string;
  confidence?: number;
}

export interface ScanProgressEvent {
  scanId: string;
  containerId: string;
  progressPercent: number;
  stage: string;
}

export interface ScanCompletedEvent {
  scanId: string;
  containerId: string;
  itemsDetected: number;
  itemsAdded: number;
  itemsRemoved: number;
}
