import {ChangeDetectionStrategy, Component} from '@angular/core';
import {RouterLink} from '@angular/router';
import {NgStyle} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';

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
          <p class="text-text-secondary mt-2 font-medium">6 контейнеров · 78 предметов</p>
        </div>
        <button class="rounded-[20px] bg-accent text-white px-6 py-3 font-semibold flex items-center gap-2 hover:scale-[0.97] transition-transform duration-200 shadow-soft">
          <mat-icon>add</mat-icon>
          Новый контейнер
        </button>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
        @for (container of containers; track container.id; let i = $index) {
          <a [routerLink]="['/container', container.id]" 
             class="group block bg-surface rounded-[24px] p-6 shadow-soft hover:shadow-soft-hover hover:-translate-y-1 transition-all duration-300 ease-[cubic-bezier(0.34,1.56,0.64,1)] relative overflow-hidden"
             [style.animation-delay.ms]="i * 50"
             class="animate-in fade-in slide-in-from-bottom-4 fill-mode-backwards">
            
            <!-- Top Accent Blob -->
            <div class="absolute -top-6 -left-6 w-32 h-32 squircle opacity-10 transition-transform duration-500 group-hover:scale-110"
                 [ngStyle]="{'background-color': container.color}"></div>
            
            <div class="relative z-10 flex items-start justify-between mb-6">
              <div class="w-16 h-16 squircle flex items-center justify-center text-white shadow-sm"
                   [ngStyle]="{'background-color': container.color}">
                <mat-icon class="text-3xl w-8 h-8 flex items-center justify-center">{{container.icon}}</mat-icon>
              </div>
            </div>

            <div class="relative z-10">
              <h3 class="text-lg font-semibold text-text-primary mb-1">{{container.name}}</h3>
              <div class="flex items-center text-text-secondary text-sm mb-4">
                <mat-icon class="text-[16px] w-4 h-4 mr-1">location_on</mat-icon>
                {{container.location}}
              </div>

              <div class="flex flex-wrap gap-2 mb-6">
                <span class="px-3 py-1 bg-mint/10 text-mint-dark text-xs font-bold rounded-full">{{container.itemsCount}} предметов</span>
                <span class="px-3 py-1 bg-violet/10 text-violet-dark text-xs font-bold rounded-full">{{container.confidence}}% уверенность</span>
                <span class="px-3 py-1 bg-gray-100 text-text-secondary text-xs font-bold rounded-full">{{container.lastUpdated}}</span>
              </div>

              <!-- Thumbnails -->
              <div class="flex -space-x-3">
                @for (item of container.previewItems; track $index) {
                  <div class="w-10 h-10 squircle-sm border-2 border-surface flex items-center justify-center text-white text-xs font-bold shadow-sm"
                       [ngStyle]="{'background-color': item.color}">
                    {{item.initial}}
                  </div>
                }
              </div>
            </div>
          </a>
        }
      </div>
    </div>
  `
})
export class HomeComponent {
  containers = [
    { id: '1', name: 'Ящик с инструментами', location: 'Гараж, полка 2', color: '#FF6B7A', icon: 'handyman', itemsCount: 14, confidence: 97, lastUpdated: '2ч назад', previewItems: [{initial: 'К', color: '#FF6B7A'}, {initial: 'Р', color: '#51E2A2'}, {initial: 'М', color: '#5BB8FF'}, {initial: 'U', color: '#9B8AFB'}] },
    { id: '2', name: 'Коробка электроники', location: 'Кабинет, шкаф', color: '#5BB8FF', icon: 'cable', itemsCount: 23, confidence: 92, lastUpdated: '1д назад', previewItems: [{initial: 'П', color: '#5BB8FF'}, {initial: 'К', color: '#FF9F43'}, {initial: 'З', color: '#38D9C4'}] },
    { id: '3', name: 'Запчасти для велосипеда', location: 'Балкон, стеллаж', color: '#51E2A2', icon: 'pedal_bike', itemsCount: 8, confidence: 99, lastUpdated: '3д назад', previewItems: [{initial: 'Ц', color: '#51E2A2'}, {initial: 'П', color: '#FF78C4'}] },
    { id: '4', name: 'Кухонные мелочи', location: 'Кухня, верхний ящик', color: '#FFD43B', icon: 'kitchen', itemsCount: 11, confidence: 85, lastUpdated: '5д назад', previewItems: [{initial: 'Н', color: '#FFD43B'}, {initial: 'В', color: '#FF6B7A'}, {initial: 'Ш', color: '#9B8AFB'}] },
    { id: '5', name: 'Travel Kit', location: 'Прихожая, полка', color: '#9B8AFB', icon: 'flight_takeoff', itemsCount: 6, confidence: 100, lastUpdated: '1н назад', previewItems: [{initial: 'П', color: '#9B8AFB'}, {initial: 'З', color: '#51E2A2'}] },
    { id: '6', name: 'Канцелярия', location: 'Кабинет, стол', color: '#FF9F43', icon: 'edit', itemsCount: 17, confidence: 94, lastUpdated: '2н назад', previewItems: [{initial: 'Р', color: '#FF9F43'}, {initial: 'С', color: '#5BB8FF'}, {initial: 'Л', color: '#FF6B7A'}] },
  ];
}
