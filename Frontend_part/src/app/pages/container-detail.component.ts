import {ChangeDetectionStrategy, Component} from '@angular/core';
import {RouterLink} from '@angular/router';
import {NgClass, NgStyle} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';

@Component({
  selector: 'app-container-detail',
  standalone: true,
  imports: [RouterLink, NgClass, NgStyle, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="animate-in fade-in slide-in-from-bottom-4 duration-500">
      <!-- Hero Banner -->
      <div class="relative w-full rounded-b-[40px] md:rounded-[40px] md:mt-6 overflow-hidden p-8 md:p-12 mb-10"
           [ngStyle]="{'background-color': container.color + '14'}"> <!-- 8% opacity approx -->
        
        <a routerLink="/" class="inline-flex items-center text-text-secondary hover:text-text-primary mb-6 transition-colors font-medium">
          <mat-icon class="mr-1 text-[20px] w-5 h-5">arrow_back</mat-icon>
          Назад
        </a>

        <div class="flex flex-col md:flex-row md:items-end justify-between gap-6">
          <div class="flex items-center gap-6">
            <div class="w-24 h-24 squircle flex items-center justify-center text-white shadow-md shrink-0"
                 [ngStyle]="{'background-color': container.color}">
              <mat-icon class="text-5xl w-12 h-12 flex items-center justify-center">{{container.icon}}</mat-icon>
            </div>
            <div>
              <h1 class="text-3xl md:text-4xl font-extrabold text-text-primary mb-2">{{container.name}}</h1>
              <div class="flex flex-wrap items-center gap-4 text-text-secondary font-medium">
                <span class="flex items-center"><mat-icon class="text-[18px] w-[18px] h-[18px] mr-1">location_on</mat-icon> {{container.location}}</span>
                <span class="flex items-center"><mat-icon class="text-[18px] w-[18px] h-[18px] mr-1">straighten</mat-icon> 350 × 200 × 400 мм</span>
                <span class="flex items-center"><mat-icon class="text-[18px] w-[18px] h-[18px] mr-1">history</mat-icon> Скан 2ч назад</span>
              </div>
            </div>
          </div>

          <div class="flex gap-3">
            <button class="rounded-[20px] border-2 border-surface bg-surface text-text-primary px-5 py-2.5 font-semibold hover:bg-gray-50 transition-colors">
              Редактировать
            </button>
            <button routerLink="/scan" class="rounded-[20px] bg-accent text-white px-5 py-2.5 font-semibold flex items-center gap-2 hover:scale-[0.97] transition-transform shadow-soft">
              <mat-icon>view_in_ar</mat-icon>
              Загрузить скан
            </button>
          </div>
        </div>
      </div>

      <div class="px-6 md:px-10 pb-20">
        <!-- Section Header -->
        <div class="flex items-center justify-between mb-6">
          <div class="flex items-center gap-3">
            <h2 class="text-xl font-bold text-text-primary">Обнаруженные предметы</h2>
            <span class="px-2.5 py-0.5 bg-surface shadow-sm rounded-full text-sm font-bold text-text-secondary">{{items.length}}</span>
          </div>
          <div class="flex bg-surface rounded-full p-1 shadow-sm">
            <button class="w-8 h-8 rounded-full flex items-center justify-center bg-surface-secondary text-accent">
              <mat-icon class="text-[20px] w-5 h-5">grid_view</mat-icon>
            </button>
            <button class="w-8 h-8 rounded-full flex items-center justify-center text-text-secondary hover:text-text-primary">
              <mat-icon class="text-[20px] w-5 h-5">view_list</mat-icon>
            </button>
          </div>
        </div>

        <!-- Items Grid -->
        <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-5 mb-12">
          @for (item of items; track item.id; let i = $index) {
            <div class="bg-surface rounded-[24px] p-5 shadow-soft hover:shadow-soft-hover hover:-translate-y-1 transition-all duration-300 relative group"
                 [style.animation-delay.ms]="i * 50"
                 class="animate-in fade-in slide-in-from-bottom-4 fill-mode-backwards">
              
              <!-- Recognition Badge -->
              <div class="absolute top-4 right-4 z-10 bg-white/90 backdrop-blur-sm px-2 py-1 squircle-sm shadow-sm flex items-center gap-1 text-[10px] font-bold text-accent">
                <mat-icon class="text-[12px] w-3 h-3">auto_awesome</mat-icon>
                AI
              </div>

              <div class="flex items-start gap-4 mb-4">
                <div class="w-14 h-14 squircle flex items-center justify-center text-white font-bold text-xl shrink-0"
                     [ngStyle]="{'background-color': item.color}">
                  {{item.name.charAt(0)}}
                </div>
                <div>
                  <h3 class="font-semibold text-text-primary leading-tight mb-2">{{item.name}}</h3>
                  <div class="flex flex-wrap gap-1.5">
                    @for (tag of item.tags; track tag) {
                      <span class="px-2 py-0.5 text-[10px] font-bold rounded-full"
                            [ngStyle]="{'background-color': item.color + '20', 'color': item.color}">
                        {{tag}}
                      </span>
                    }
                  </div>
                </div>
              </div>

              <!-- Confidence indicator -->
              <div class="flex items-center justify-between mt-auto pt-4 border-t border-gray-100">
                <div class="w-16 h-1.5 rounded-full bg-gray-100 overflow-hidden">
                  <div class="h-full rounded-full" 
                       [ngClass]="item.confidence > 90 ? 'bg-success' : (item.confidence > 70 ? 'bg-orange-400' : 'bg-danger')"
                       [style.width.%]="item.confidence"></div>
                </div>
                <span class="font-mono text-xs text-text-secondary">{{item.confidence}}%</span>
              </div>
            </div>
          }
        </div>

        <!-- Scan Timeline -->
        <h2 class="text-xl font-bold text-text-primary mb-4">История сканирований</h2>
        <div class="flex gap-4 overflow-x-auto pb-4 hide-scrollbar">
          <div class="shrink-0 w-64 bg-surface rounded-[20px] p-4 shadow-soft relative overflow-hidden">
            <!-- Animated gradient border effect for most recent -->
            <div class="absolute inset-0 bg-gradient-to-r from-accent to-violet opacity-20 -z-10"></div>
            <div class="absolute inset-[2px] bg-surface rounded-[18px] -z-10"></div>
            
            <div class="flex items-center gap-3 mb-2">
              <div class="w-8 h-8 squircle-sm bg-accent/10 text-accent flex items-center justify-center">
                <mat-icon class="text-[18px] w-[18px] h-[18px]">view_in_ar</mat-icon>
              </div>
              <div>
                <div class="text-sm font-bold text-text-primary">Сегодня, 14:30</div>
                <div class="text-xs text-text-secondary">3D Скан</div>
              </div>
            </div>
            <div class="text-sm font-medium text-success flex items-center gap-1">
              <mat-icon class="text-[16px] w-4 h-4">check_circle</mat-icon>
              14 обнаружено
            </div>
          </div>

          <div class="shrink-0 w-64 bg-surface rounded-[20px] p-4 shadow-soft">
            <div class="flex items-center gap-3 mb-2">
              <div class="w-8 h-8 squircle-sm bg-gray-100 text-text-secondary flex items-center justify-center">
                <mat-icon class="text-[18px] w-[18px] h-[18px]">photo_camera</mat-icon>
              </div>
              <div>
                <div class="text-sm font-bold text-text-primary">12 Марта</div>
                <div class="text-xs text-text-secondary">Фото</div>
              </div>
            </div>
            <div class="text-sm font-medium text-text-secondary flex items-center gap-1">
              <mat-icon class="text-[16px] w-4 h-4">inventory_2</mat-icon>
              12 обнаружено
            </div>
          </div>
        </div>
      </div>
    </div>
  `
})
export class ContainerDetailComponent {
  container = { id: '1', name: 'Ящик с инструментами', location: 'Гараж, полка 2', color: '#FF6B7A', icon: 'handyman' };
  
  items = [
    { id: '1', name: 'Крестовая отвёртка PH2', tags: ['инструмент', 'отвёртка'], confidence: 97, color: '#FF6B7A' },
    { id: '2', name: 'Разводной ключ 250мм', tags: ['инструмент', 'ключ'], confidence: 94, color: '#FF9F43' },
    { id: '3', name: 'Мультиметр DT830B', tags: ['электроника', 'измерение'], confidence: 91, color: '#5BB8FF' },
    { id: '4', name: 'USB-C переходник', tags: ['электроника', 'кабель'], confidence: 88, color: '#9B8AFB' },
    { id: '5', name: 'Изолента чёрная', tags: ['расходник', 'изоляция'], confidence: 95, color: '#38D9C4' },
    { id: '6', name: 'Набор бит (12 шт)', tags: ['инструмент', 'биты'], confidence: 82, color: '#51E2A2' },
    { id: '7', name: 'Рулетка 5м', tags: ['инструмент', 'измерение'], confidence: 96, color: '#FFD43B' },
  ];
}
