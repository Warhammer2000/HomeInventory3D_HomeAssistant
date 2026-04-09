export interface ContainerDto {
  id: string;
  name: string;
  nfcId?: string;
  qrCode?: string;
  location: string;
  description?: string;
  widthMm?: number;
  heightMm?: number;
  depthMm?: number;
  meshFilePath?: string;
  thumbnailPath?: string;
  itemCount: number;
  createdAt: string;
  updatedAt: string;
  lastScannedAt?: string;
}

export interface CreateContainerDto {
  name: string;
  location: string;
  nfcId?: string;
  qrCode?: string;
  description?: string;
  widthMm?: number;
  heightMm?: number;
  depthMm?: number;
}

export interface UpdateContainerDto {
  name: string;
  location: string;
  nfcId?: string;
  qrCode?: string;
  description?: string;
  widthMm?: number;
  heightMm?: number;
  depthMm?: number;
}
