import {ChangeDetectionStrategy, Component, inject, OnInit, signal} from '@angular/core';
import {RouterLink} from '@angular/router';
import {NgStyle} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {ContainerService} from '../services/container.service';
import {ContainerDto} from '../models/container.model';

const COLORS = ['#FF6B7A', '#5BB8FF', '#51E2A2', '#FFD43B', '#9B8AFB', '#FF9F43', '#FF78C4', '#38D9C4'];
const ICONS = ['inventory_2', 'handyman', 'cable', 'kitchen', 'pedal_bike', 'flight_takeoff', 'edit', 'category'];

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [RouterLink, NgStyle, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-6 md:p-10 animate-in fade-in slide-in-from-bottom-4 duration-500">
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 mb-10">
        <div>
          <h1 class="text-3xl md:text-4xl font-extrabold text-text-primary">Мои контейнеры</h1>
          <p class="text-text-secondary mt-2 font-medium">
            {{containers().length}} контейнеров · {{totalItems()}} предметов
          </p>
        </div>
        <button (click)="createContainer()"
                class="rounded-[20px] bg-accent text-white px-6 py-3 font-semibold flex items-center gap-2 hover:scale-[0.97] transition-transform duration-200 shadow-soft">
          <mat-icon>add</mat-icon>
          Новый контейнер
        </button>
      </div>

      @if (loading()) {
        <div class="flex justify-center py-20">
          <div class="w-10 h-10 border-4 border-accent/20 border-t-accent rounded-full animate-spin"></div>
        </div>
      } @else if (containers().length === 0) {
        <div class="flex flex-col items-center justify-center py-20 text-center">
          <div class="w-32 h-32 squircle bg-surface-secondary text-accent flex items-center justify-center mb-6">
            <mat-icon class="text-6xl w-16 h-16 flex items-center justify-center">inventory_2</mat-icon>
          </div>
          <h3 class="text-xl font-bold text-text-primary mb-2">Пока нет контейнеров</h3>
          <p class="text-text-secondary font-medium">Создайте первый контейнер чтобы начать</p>
        </div>
      } @else {
        <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          @for (container of containers(); track container.id; let i = $index) {
            <a [routerLink]="['/container', container.id]"
               class="group block bg-surface rounded-[24px] p-6 shadow-soft hover:shadow-soft-hover hover:-translate-y-1 transition-all duration-300 ease-[cubic-bezier(0.34,1.56,0.64,1)] relative overflow-hidden animate-in fade-in slide-in-from-bottom-4 fill-mode-backwards"
               [style.animation-delay.ms]="i * 50">

              <div class="absolute -top-6 -left-6 w-32 h-32 squircle opacity-10 transition-transform duration-500 group-hover:scale-110"
                   [ngStyle]="{'background-color': getColor(i)}"></div>

              <div class="relative z-10 flex items-start justify-between mb-6">
                <div class="w-16 h-16 squircle flex items-center justify-center text-white shadow-sm"
                     [ngStyle]="{'background-color': getColor(i)}">
                  <mat-icon class="text-3xl w-8 h-8 flex items-center justify-center">{{getIcon(i)}}</mat-icon>
                </div>
              </div>

              <div class="relative z-10">
                <h3 class="text-lg font-semibold text-text-primary mb-1">{{container.name}}</h3>
                <div class="flex items-center text-text-secondary text-sm mb-4">
                  <mat-icon class="text-[16px] w-4 h-4 mr-1">location_on</mat-icon>
                  {{container.location}}
                </div>

                <div class="flex flex-wrap gap-2 mb-4">
                  <span class="px-3 py-1 bg-mint/10 text-mint-dark text-xs font-bold rounded-full">
                    {{container.itemCount}} предметов
                  </span>
                  @if (container.lastScannedAt) {
                    <span class="px-3 py-1 bg-gray-100 text-text-secondary text-xs font-bold rounded-full">
                      {{formatDate(container.lastScannedAt)}}
                    </span>
                  }
                </div>

                @if (container.description) {
                  <p class="text-text-secondary text-sm line-clamp-2">{{container.description}}</p>
                }
              </div>
            </a>
          }
        </div>
      }
    </div>
  `
})
export class HomeComponent implements OnInit {
  private containerService = inject(ContainerService);

  containers = signal<ContainerDto[]>([]);
  loading = signal(true);
  totalItems = signal(0);

  ngOnInit() {
    this.loadContainers();
  }

  loadContainers() {
    this.loading.set(true);
    this.containerService.getAll().subscribe({
      next: (data) => {
        this.containers.set(data);
        this.totalItems.set(data.reduce((sum, c) => sum + c.itemCount, 0));
        this.loading.set(false);
      },
      error: (err) => {
        console.error('Failed to load containers:', err);
        this.loading.set(false);
      }
    });
  }

  createContainer() {
    const name = prompt('Название контейнера:');
    if (!name) return;
    const location = prompt('Расположение:');
    if (!location) return;

    this.containerService.create({name, location}).subscribe({
      next: () => this.loadContainers(),
      error: (err) => console.error('Failed to create container:', err)
    });
  }

  getColor(index: number): string {
    return COLORS[index % COLORS.length];
  }

  getIcon(index: number): string {
    return ICONS[index % ICONS.length];
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
    return `${Math.floor(diffDays / 7)}н назад`;
  }
}
