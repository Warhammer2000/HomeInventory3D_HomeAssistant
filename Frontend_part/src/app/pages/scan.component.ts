import {ChangeDetectionStrategy, Component, computed, effect, inject, OnDestroy, OnInit, signal} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {DecimalPipe, NgStyle} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {ScanService} from '../services/scan.service';
import {SignalRService} from '../services/signalr.service';
import {ContainerService} from '../services/container.service';
import {ContainerDto} from '../models/container.model';
import {ItemAddedEvent} from '../models/signalr-events.model';

@Component({
  selector: 'app-scan',
  standalone: true,
  imports: [NgStyle, MatIconModule, DecimalPipe],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-6 md:p-10 h-full flex flex-col max-w-3xl mx-auto animate-in fade-in slide-in-from-bottom-4 duration-500">
      <h1 class="text-3xl md:text-4xl font-extrabold text-text-primary mb-8 text-center">Новый скан</h1>

      @if (!containerId()) {
        <!-- Container Selection -->
        <div class="flex-1 flex flex-col items-center justify-center">
          <p class="text-text-secondary font-medium mb-6">Выберите контейнер для сканирования</p>
          <div class="flex flex-col gap-3 w-full max-w-md">
            @for (c of containers(); track c.id) {
              <button (click)="selectContainer(c.id)"
                      class="bg-surface rounded-[20px] p-4 shadow-soft hover:shadow-soft-hover transition-all text-left flex items-center gap-4">
                <mat-icon class="text-accent">inventory_2</mat-icon>
                <div>
                  <div class="font-semibold text-text-primary">{{c.name}}</div>
                  <div class="text-sm text-text-secondary">{{c.location}}</div>
                </div>
              </button>
            }
          </div>
        </div>
      } @else if (state() === 'upload') {
        <!-- Mode Toggle -->
        <div class="flex justify-center mb-8">
          <div class="flex bg-surface rounded-full p-1.5 shadow-soft gap-1">
            <button (click)="mode.set('3d')"
                    class="px-5 py-2.5 rounded-full font-semibold text-sm flex items-center gap-2 transition-all"
                    [class]="mode() === '3d' ? 'bg-accent text-white shadow-sm' : 'text-text-secondary hover:text-text-primary'">
              <mat-icon class="text-[18px] w-[18px] h-[18px]">view_in_ar</mat-icon>
              3D файл
            </button>
            <button (click)="mode.set('photo')"
                    class="px-5 py-2.5 rounded-full font-semibold text-sm flex items-center gap-2 transition-all"
                    [class]="mode() === 'photo' ? 'bg-accent text-white shadow-sm' : 'text-text-secondary hover:text-text-primary'">
              <mat-icon class="text-[18px] w-[18px] h-[18px]">photo_camera</mat-icon>
              Фото предмета
            </button>
          </div>
        </div>

        <!-- Upload State -->
        <div class="flex-1 flex flex-col items-center justify-center">
          <input #fileInput type="file"
                 [accept]="mode() === 'photo' ? '.jpg,.jpeg,.png,.webp,.heic' : '.obj,.ply,.glb,.gltf,.fbx,.3ds'"
                 class="hidden"
                 (change)="onFileSelected($event)">

          <div class="w-full max-w-lg aspect-square squircle-lg border-4 border-dashed border-accent/30 bg-surface hover:bg-accent/5 hover:border-accent transition-all duration-300 flex flex-col items-center justify-center p-8 cursor-pointer group shadow-sm hover:shadow-soft"
               (click)="fileInput.click()"
               (dragover)="onDragOver($event)"
               (drop)="onDrop($event)">

            <div class="w-40 h-40 mb-8 relative group-hover:-translate-y-2 transition-transform duration-500 ease-[cubic-bezier(0.34,1.56,0.64,1)]">
              <div class="absolute inset-0 bg-accent/20 squircle blur-xl"></div>
              <div class="relative w-full h-full bg-gradient-to-br from-sky to-accent squircle flex items-center justify-center shadow-lg">
                <mat-icon class="text-white text-6xl w-16 h-16 flex items-center justify-center">{{mode() === 'photo' ? 'photo_camera' : 'view_in_ar'}}</mat-icon>
              </div>
            </div>

            <h2 class="text-2xl font-bold text-text-primary mb-2 text-center group-hover:text-accent transition-colors">
              {{mode() === 'photo' ? 'Перетащите фото сюда' : 'Перетащите файл сюда'}}
            </h2>
            <p class="text-text-secondary text-center font-medium">
              {{mode() === 'photo' ? 'или нажмите для выбора · JPG, PNG, WEBP' : 'или нажмите для выбора · OBJ, PLY, GLB, FBX, GLTF, 3DS'}}
            </p>
          </div>
        </div>
      } @else {
        <!-- Processing State -->
        <div class="flex flex-col items-center w-full animate-in zoom-in-95 duration-500">

          <div class="relative w-48 h-48 mb-6">
            <svg class="w-full h-full -rotate-90" viewBox="0 0 100 100">
              <circle cx="50" cy="50" r="40" fill="none" stroke="currentColor" stroke-width="8" class="text-surface-secondary"></circle>
              <circle cx="50" cy="50" r="40" fill="none" stroke="url(#gradient)" stroke-width="8" stroke-linecap="round"
                      class="transition-all duration-300 ease-out"
                      [style.stroke-dasharray]="251.2"
                      [style.stroke-dashoffset]="251.2 - (251.2 * progress()) / 100"></circle>
              <defs>
                <linearGradient id="gradient" x1="0%" y1="0%" x2="100%" y2="100%">
                  <stop offset="0%" stop-color="#4F46E5" />
                  <stop offset="100%" stop-color="#9B8AFB" />
                </linearGradient>
              </defs>
            </svg>

            <div class="absolute inset-0 flex flex-col items-center justify-center">
              <span class="text-4xl font-extrabold text-text-primary">{{progress()}}%</span>
            </div>

            @if (progress() === 100) {
              <div class="absolute inset-0 pointer-events-none">
                @for (i of [1,2,3,4,5,6,7,8]; track i) {
                  <div class="absolute top-1/2 left-1/2 w-3 h-3 rounded-full animate-confetti"
                       [ngStyle]="{'background-color': ['#FF6B7A', '#FFD43B', '#51E2A2', '#5BB8FF'][i%4], '--angle': (i * 45) + 'deg'}"></div>
                }
              </div>
            }
          </div>

          @if (errorMessage()) {
            <p class="text-danger font-medium mb-4">{{errorMessage()}}</p>
          }

          <!-- Per-item Progress Bars -->
          @if (itemProgressMap().size > 0) {
            <div class="w-full bg-surface rounded-[24px] p-5 shadow-soft mb-6">
              <div class="flex items-center gap-2 mb-4">
                <mat-icon class="text-accent text-[18px] w-[18px] h-[18px]">view_in_ar</mat-icon>
                <h3 class="font-bold text-text-primary text-sm">3D Generation</h3>
              </div>
              <div class="flex flex-col gap-3">
                @for (entry of itemProgressEntries(); track entry.name) {
                  <div class="flex flex-col gap-1.5">
                    <div class="flex items-center justify-between">
                      <span class="text-sm font-semibold text-text-primary truncate flex-1">{{entry.name}}</span>
                      <span class="text-xs font-mono ml-2"
                            [class]="entry.percent >= 100 ? 'text-success font-bold' : 'text-text-secondary'">
                        {{entry.percent >= 100 ? '✅ Done' : entry.stage + ' ' + entry.percent + '%'}}
                      </span>
                    </div>
                    <div class="w-full h-2 rounded-full bg-gray-100 overflow-hidden">
                      <div class="h-full rounded-full transition-all duration-500 ease-out"
                           [style.width.%]="entry.percent"
                           [class]="entry.percent >= 100 ? 'bg-success' : entry.percent > 50 ? 'bg-accent' : 'bg-sky'">
                      </div>
                    </div>
                  </div>
                }
              </div>
            </div>
          }

          <!-- Processing Log (compact) -->
          <div class="w-full bg-surface rounded-[24px] p-4 shadow-soft mb-6 max-h-[150px] overflow-y-auto hide-scrollbar">
            <div class="flex flex-col gap-1">
              @for (log of logMessages(); track $index) {
                <div class="text-[11px] font-mono text-text-secondary/70 leading-relaxed">
                  <span class="text-text-secondary/40">{{log.percent}}%</span> {{log.message}}
                </div>
              }
            </div>
          </div>

          <!-- Found Items -->
          <div class="w-full bg-surface rounded-[32px] p-6 shadow-soft min-h-[200px]">
            <div class="flex items-center justify-between mb-4">
              <h3 class="font-bold text-text-primary">Готово: {{foundItems().length}} предметов</h3>
              @if (progress() === 100) {
                <span class="text-xs font-bold text-success bg-success/10 px-3 py-1 rounded-full">Complete</span>
              }
            </div>

            <div class="flex flex-col gap-3">
              @for (item of foundItems(); track item.id) {
                <div class="flex items-center gap-4 p-3 rounded-[20px] bg-cream/50 animate-in slide-in-from-bottom-2 fade-in duration-300">
                  <div class="w-12 h-12 squircle flex items-center justify-center text-white font-bold shrink-0"
                       [ngStyle]="{'background-color': ['#FF6B7A','#FF9F43','#5BB8FF','#9B8AFB','#51E2A2','#38D9C4'][$index % 6]}">
                    {{item.name.charAt(0)}}
                  </div>
                  <div class="flex-1 min-w-0">
                    <div class="font-semibold text-text-primary truncate">{{item.name}}</div>
                    <div class="flex items-center gap-2 mt-1">
                      <span class="text-xs font-mono text-text-secondary">{{(item.confidence ?? 0) * 100 | number:'1.0-0'}}%</span>
                      @if (item.materialType) {
                        <span class="text-[10px] px-2 py-0.5 rounded-full bg-accent/10 text-accent font-bold">{{item.materialType}}</span>
                      }
                      @if (item.meshUrl) {
                        <span class="text-[10px] px-2 py-0.5 rounded-full bg-success/10 text-success font-bold">3D ✓</span>
                      }
                    </div>
                  </div>
                  <div class="w-8 h-8 rounded-full bg-success/10 text-success flex items-center justify-center">
                    <mat-icon class="text-[16px] w-4 h-4">check</mat-icon>
                  </div>
                </div>
              }
            </div>
          </div>

          @if (progress() === 100) {
            <button (click)="viewContainer()"
                    class="mt-8 rounded-[20px] bg-accent text-white px-8 py-4 font-bold text-lg flex items-center gap-2 hover:scale-[0.97] transition-transform shadow-soft animate-in slide-in-from-bottom-4 fade-in">
              Посмотреть контейнер
              <mat-icon>arrow_forward</mat-icon>
            </button>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .animate-confetti {
      animation: explode 1s ease-out forwards;
    }
    @keyframes explode {
      0% { transform: translate(-50%, -50%) scale(0); opacity: 1; }
      100% { transform: translate(calc(-50% + cos(var(--angle)) * 100px), calc(-50% + sin(var(--angle)) * 100px)) scale(1); opacity: 0; }
    }
  `]
})
export class ScanComponent implements OnInit, OnDestroy {
  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private scanService = inject(ScanService);
  private signalR = inject(SignalRService);
  private containerService = inject(ContainerService);

  containerId = signal<string | null>(null);
  containers = signal<ContainerDto[]>([]);
  mode = signal<'3d' | 'photo'>('photo');
  state = signal<'upload' | 'processing'>('upload');
  progress = signal(0);
  stage = signal('Загрузка...');
  foundItems = signal<ItemAddedEvent[]>([]);
  errorMessage = signal<string | null>(null);
  logMessages = signal<{percent: number; message: string}[]>([]);
  itemProgressMap = signal<Map<string, {name: string; percent: number; stage: string}>>(new Map());
  itemProgressEntries = computed(() => Array.from(this.itemProgressMap().values()));

  private progressEffect = effect(() => {
    const p = this.signalR.scanProgress();
    if (p) {
      console.log('SignalR progress:', p.percent, p.stage, 'container:', p.containerId, 'watching:', this.containerId());
      this.progress.set(p.percent);
      this.stage.set(p.stage);
      const logs = this.logMessages();
      if (logs.length === 0 || logs[logs.length - 1].message !== p.stage) {
        this.logMessages.update(l => [...l, {percent: p.percent, message: p.stage}]);
      }
    }
  });

  private itemEffect = effect(() => {
    const item = this.signalR.itemAdded();
    if (item) {
      console.log('SignalR itemAdded:', item.name, 'container:', item.containerId);
      this.foundItems.update(items => [...items, item]);
    }
  });

  private itemProgressEffect = effect(() => {
    const p = this.signalR.itemProgress();
    if (p) {
      this.itemProgressMap.update(map => {
        const newMap = new Map(map);
        newMap.set(p.itemName, {name: p.itemName, percent: p.percent, stage: p.stage});
        return newMap;
      });
    }
  });

  private completedEffect = effect(() => {
    const c = this.signalR.scanCompleted();
    if (c) {
      console.log('SignalR completed:', c);
      this.progress.set(100);
      this.stage.set('Готово');
    }
  });

  private failedEffect = effect(() => {
    const f = this.signalR.scanFailed();
    if (f) {
      this.errorMessage.set(f.errorMessage);
    }
  });

  ngOnInit() {
    const cId = this.route.snapshot.queryParamMap.get('containerId');
    if (cId) {
      this.containerId.set(cId);
      this.initSignalR(cId);
    } else {
      this.containerService.getAll().subscribe(c => this.containers.set(c));
    }
  }

  ngOnDestroy() {
    const cId = this.containerId();
    if (cId) {
      this.signalR.leaveContainer(cId);
    }
  }

  selectContainer(id: string) {
    this.containerId.set(id);
    this.initSignalR(id);
  }

  private async initSignalR(containerId: string) {
    await this.signalR.connect();
    await this.signalR.joinContainer(containerId);
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.uploadFile(input.files[0]);
    }
  }

  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer?.files.length) {
      this.uploadFile(event.dataTransfer.files[0]);
    }
  }

  private uploadFile(file: File) {
    const cId = this.containerId();
    if (!cId) return;

    this.state.set('processing');
    this.progress.set(0);
    this.foundItems.set([]);
    this.errorMessage.set(null);
    this.logMessages.set([{percent: 0, message: '📤 Uploading file...'}]);
    this.stage.set('Загрузка файла...');

    const scanType = this.mode() === 'photo' ? 'Photo' as const : 'Lidar' as const;
    this.scanService.upload(file, cId, scanType).subscribe({
      next: () => {
        this.stage.set('Обработка...');
      },
      error: (err) => {
        this.errorMessage.set('Ошибка загрузки: ' + (err.error?.title || err.message));
        console.error('Upload failed:', err);
      }
    });
  }

  viewContainer() {
    this.router.navigate(['/container', this.containerId()]);
  }
}
