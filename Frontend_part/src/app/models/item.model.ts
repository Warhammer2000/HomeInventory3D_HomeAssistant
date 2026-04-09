export type ItemStatus = 'Present' | 'Removed' | 'Moved';
export type RecognitionSource = 'ClaudeVision' | 'Yolo' | 'Manual';

export interface ItemDto {
  id: string;
  containerId: string;
  name: string;
  tags: string[];
  description?: string;
  positionX?: number;
  positionY?: number;
  positionZ?: number;
  bboxMinX?: number;
  bboxMinY?: number;
  bboxMinZ?: number;
  bboxMaxX?: number;
  bboxMaxY?: number;
  bboxMaxZ?: number;
  rotationX?: number;
  rotationY?: number;
  rotationZ?: number;
  photoPath?: string;
  meshFilePath?: string;
  thumbnailPath?: string;
  confidence?: number;
  recognitionSource?: RecognitionSource;
  status: ItemStatus;
  createdAt: string;
  updatedAt: string;
}

export interface CreateItemDto {
  containerId: string;
  name: string;
  tags?: string[];
  description?: string;
  positionX?: number;
  positionY?: number;
  positionZ?: number;
}

export interface UpdateItemDto {
  name: string;
  tags?: string[];
  description?: string;
  positionX?: number;
  positionY?: number;
  positionZ?: number;
}

export interface UpdateItemStatusDto {
  status: ItemStatus;
}
