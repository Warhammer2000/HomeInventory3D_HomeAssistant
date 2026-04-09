import {ChangeDetectionStrategy, Component, inject, OnInit, signal} from '@angular/core';
import {ActivatedRoute, RouterLink} from '@angular/router';
import {NgClass, NgStyle} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {ContainerService} from '../services/container.service';
import {ItemService} from '../services/item.service';
import {ScanService} from '../services/scan.service';
import {ContainerDto} from '../models/container.model';
import {ItemDto} from '../models/item.model';
import {ScanSessionDto} from '../models/scan.model';

const COLORS = ['#FF6B7A', '#FF9F43', '#5BB8FF', '#9B8AFB', '#38D9C4', '#51E2A2', '#FFD43B', '#FF78C4'];

@Component({
  selector: 'app-container-detail',
  standalone: true,
  imports: [RouterLink, NgClass, NgStyle, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="animate-in fade-in slide-in-from-bottom-4 duration-500">
      @if (loading()) {
        <div class="flex justify-center py-20">
          <div class="w-10 h-10 border-4 border-accent/20 border-t-accent rounded-full animate-spin"></div>
        </div>
      } @else if (container()) {
        <!-- Hero Banner -->
        <div class="relative w-full rounded-b-[40px] md:rounded-[40px] md:mt-6 overflow-hidden p-8 md:p-12 mb-10"
             [ngStyle]="{'background-color': getColor(0) + '14'}">

          <a routerLink="/" class="inline-flex items-center text-text-secondary hover:text-text-primary mb-6 transition-colors font-medium">
            <mat-icon class="mr-1 text-[20px] w-5 h-5">arrow_back</mat-icon>
            Назад
          </a>

          <div class="flex flex-col md:flex-row md:items-end justify-between gap-6">
            <div class="flex items-center gap-6">
              <div class="w-24 h-24 squircle flex items-center justify-center text-white shadow-md shrink-0"
                   [ngStyle]="{'background-color': getColor(0)}">
                <mat-icon class="text-5xl w-12 h-12 flex items-center justify-center">inventory_2</mat-icon>
              </div>
              <div>
                <h1 class="text-3xl md:text-4xl font-extrabold text-text-primary mb-2">{{container()!.name}}</h1>
                <div class="flex flex-wrap items-center gap-4 text-text-secondary font-medium">
                  <span class="flex items-center"><mat-icon class="text-[18px] w-[18px] h-[18px] mr-1">location_on</mat-icon> {{container()!.location}}</span>
                  @if (container()!.widthMm && container()!.heightMm && container()!.depthMm) {
                    <span class="flex items-center"><mat-icon class="text-[18px] w-[18px] h-[18px] mr-1">straighten</mat-icon> {{container()!.widthMm}} × {{container()!.heightMm}} × {{container()!.depthMm}} мм</span>
                  }
                  @if (container()!.lastScannedAt) {
                    <span class="flex items-center"><mat-icon class="text-[18px] w-[18px] h-[18px] mr-1">history</mat-icon> Скан {{formatDate(container()!.lastScannedAt!)}}</span>
                  }
                </div>
              </div>
            </div>

            <div class="flex gap-3">
              <a [routerLink]="['/scan']" [queryParams]="{containerId: container()!.id}"
                 class="rounded-[20px] bg-accent text-white px-5 py-2.5 font-semibold flex items-center gap-2 hover:scale-[0.97] transition-transform shadow-soft">
                <mat-icon>view_in_ar</mat-icon>
                Загрузить скан
              </a>
            </div>
          </div>
        </div>

        <div class="px-6 md:px-10 pb-20">
          <!-- Items -->
          <div class="flex items-center justify-between mb-6">
            <div class="flex items-center gap-3">
              <h2 class="text-xl font-bold text-text-primary">Обнаруженные предметы</h2>
              <span class="px-2.5 py-0.5 bg-surface shadow-sm rounded-full text-sm font-bold text-text-secondary">{{items().length}}</span>
            </div>
          </div>

          @if (items().length === 0) {
            <div class="flex flex-col items-center justify-center py-16 text-center">
              <div class="w-24 h-24 squircle bg-surface-secondary text-text-secondary flex items-center justify-center mb-4">
                <mat-icon class="text-5xl w-12 h-12">inventory</mat-icon>
              </div>
              <p class="text-text-secondary font-medium">Пока нет предметов. Загрузите 3D-скан!</p>
            </div>
          } @else {
            <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5 mb-12">
              @for (item of items(); track item.id; let i = $index) {
                <div class="bg-surface rounded-[24px] p-5 shadow-soft hover:shadow-soft-hover hover:-translate-y-1 transition-all duration-300 relative group animate-in fade-in slide-in-from-bottom-4 fill-mode-backwards"
                     [style.animation-delay.ms]="i * 50">

                  @if (item.recognitionSource) {
                    <div class="absolute top-4 right-4 z-10 bg-white/90 backdrop-blur-sm px-2 py-1 squircle-sm shadow-sm flex items-center gap-1 text-[10px] font-bold text-accent">
                      <mat-icon class="text-[12px] w-3 h-3">auto_awesome</mat-icon>
                      AI
                    </div>
                  }

                  <div class="flex items-start gap-4 mb-4">
                    <div class="w-14 h-14 squircle flex items-center justify-center text-white font-bold text-xl shrink-0"
                         [ngStyle]="{'background-color': getColor(i)}">
                      {{item.name.charAt(0)}}
                    </div>
                    <div>
                      <h3 class="font-semibold text-text-primary leading-tight mb-2">{{item.name}}</h3>
                      <div class="flex flex-wrap gap-1.5">
                        @for (tag of item.tags; track tag) {
                          <span class="px-2 py-0.5 text-[10px] font-bold rounded-full"
                                [ngStyle]="{'background-color': getColor(i) + '20', 'color': getColor(i)}">
                            {{tag}}
                          </span>
                        }
                      </div>
                    </div>
                  </div>

                  @if (item.confidence) {
                    <div class="flex items-center justify-between mt-auto pt-4 border-t border-gray-100">
                      <div class="w-16 h-1.5 rounded-full bg-gray-100 overflow-hidden">
                        <div class="h-full rounded-full"
                             [ngClass]="item.confidence > 90 ? 'bg-success' : (item.confidence > 70 ? 'bg-orange-400' : 'bg-danger')"
                             [style.width.%]="item.confidence"></div>
                      </div>
                      <span class="font-mono text-xs text-text-secondary">{{item.confidence}}%</span>
                    </div>
                  }
                </div>
              }
            </div>
          }

          <!-- Scan Timeline -->
          @if (scans().length > 0) {
            <h2 class="text-xl font-bold text-text-primary mb-4">История сканирований</h2>
            <div class="flex gap-4 overflow-x-auto pb-4 hide-scrollbar">
              @for (scan of scans(); track scan.id; let i = $index) {
                <div class="shrink-0 w-64 bg-surface rounded-[20px] p-4 shadow-soft relative overflow-hidden">
                  @if (i === 0) {
                    <div class="absolute inset-0 bg-gradient-to-r from-accent to-violet opacity-20 -z-10"></div>
                    <div class="absolute inset-[2px] bg-surface rounded-[18px] -z-10"></div>
                  }

                  <div class="flex items-center gap-3 mb-2">
                    <div class="w-8 h-8 squircle-sm flex items-center justify-center"
                         [ngClass]="i === 0 ? 'bg-accent/10 text-accent' : 'bg-gray-100 text-text-secondary'">
                      <mat-icon class="text-[18px] w-[18px] h-[18px]">view_in_ar</mat-icon>
                    </div>
                    <div>
                      <div class="text-sm font-bold text-text-primary">{{formatDate(scan.scannedAt)}}</div>
                      <div class="text-xs text-text-secondary">{{scan.scanType}} скан</div>
                    </div>
                  </div>
                  <div class="text-sm font-medium flex items-center gap-1"
                       [ngClass]="scan.status === 'Completed' ? 'text-success' : scan.status === 'Failed' ? 'text-danger' : 'text-text-secondary'">
                    <mat-icon class="text-[16px] w-4 h-4">
                      {{scan.status === 'Completed' ? 'check_circle' : scan.status === 'Failed' ? 'error' : 'pending'}}
                    </mat-icon>
                    {{scan.itemsAdded}} добавлено
                  </div>
                </div>
              }
            </div>
          }
        </div>
      }
    </div>
  `
})
export class ContainerDetailComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private containerService = inject(ContainerService);
  private itemService = inject(ItemService);
  private scanService = inject(ScanService);

  container = signal<ContainerDto | null>(null);
  items = signal<ItemDto[]>([]);
  scans = signal<ScanSessionDto[]>([]);
  loading = signal(true);

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) this.loadData(id);
  }

  private loadData(id: string) {
    this.loading.set(true);
    this.containerService.getById(id).subscribe({
      next: (c) => {
        this.container.set(c);
        this.loading.set(false);
      },
      error: () => this.loading.set(false)
    });
    this.itemService.getByContainer(id).subscribe({
      next: (items) => this.items.set(items)
    });
    this.scanService.getHistory(id).subscribe({
      next: (scans) => this.scans.set(scans)
    });
  }

  getColor(index: number): string {
    return COLORS[index % COLORS.length];
  }

  formatDate(dateStr: string): string {
    const date = new Date(dateStr);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffHours = Math.floor(diffMs / 3600000);
    if (diffHours < 1) return 'Только что';
    if (diffHours < 24) return `${diffHours}ч назад`;
    const diffDays = Math.floor(diffHours / 24);
    if (diffDays < 7) return `${diffDays}д назад`;
    return date.toLocaleDateString('ru-RU', {day: 'numeric', month: 'long'});
  }
}
