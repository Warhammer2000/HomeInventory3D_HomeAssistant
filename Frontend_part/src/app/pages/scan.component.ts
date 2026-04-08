import {ChangeDetectionStrategy, Component, signal} from '@angular/core';
import {NgStyle} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';

@Component({
  selector: 'app-scan',
  standalone: true,
  imports: [NgStyle, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-6 md:p-10 h-full flex flex-col max-w-3xl mx-auto animate-in fade-in slide-in-from-bottom-4 duration-500">
      <h1 class="text-3xl md:text-4xl font-extrabold text-text-primary mb-8 text-center">Новый скан</h1>

      @if (state() === 'upload') {
        <!-- Upload State -->
        <div class="flex-1 flex flex-col items-center justify-center">
          <div class="w-full max-w-lg aspect-square squircle-lg border-4 border-dashed border-accent/30 bg-surface hover:bg-accent/5 hover:border-accent transition-all duration-300 flex flex-col items-center justify-center p-8 cursor-pointer group shadow-sm hover:shadow-soft"
               (click)="startScan()">
            
            <!-- 3D Clay-like Illustration Placeholder -->
            <div class="w-40 h-40 mb-8 relative group-hover:-translate-y-2 transition-transform duration-500 ease-[cubic-bezier(0.34,1.56,0.64,1)]">
              <div class="absolute inset-0 bg-accent/20 squircle blur-xl"></div>
              <div class="relative w-full h-full bg-gradient-to-br from-sky to-accent squircle flex items-center justify-center shadow-lg">
                <mat-icon class="text-white text-6xl w-16 h-16 flex items-center justify-center">view_in_ar</mat-icon>
              </div>
            </div>

            <h2 class="text-2xl font-bold text-text-primary mb-2 text-center group-hover:text-accent transition-colors">Перетащите файл сюда</h2>
            <p class="text-text-secondary text-center font-medium">или нажмите для выбора · OBJ, PLY, GLB, JPG, PNG</p>
          </div>
        </div>
      } @else {
        <!-- Processing State -->
        <div class="flex flex-col items-center w-full animate-in zoom-in-95 duration-500">
          
          <!-- Progress Ring -->
          <div class="relative w-48 h-48 mb-6">
            <!-- Background ring -->
            <svg class="w-full h-full -rotate-90" viewBox="0 0 100 100">
              <circle cx="50" cy="50" r="40" fill="none" stroke="currentColor" stroke-width="8" class="text-surface-secondary"></circle>
              <!-- Progress ring -->
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

            <!-- Confetti (shown when complete) -->
            @if (progress() === 100) {
              <div class="absolute inset-0 pointer-events-none">
                @for (i of [1,2,3,4,5,6,7,8]; track i) {
                  <div class="absolute top-1/2 left-1/2 w-3 h-3 rounded-full animate-confetti"
                       [ngStyle]="{'background-color': ['#FF6B7A', '#FFD43B', '#51E2A2', '#5BB8FF'][i%4], '--angle': (i * 45) + 'deg'}"></div>
                }
              </div>
            }
          </div>

          <p class="text-text-secondary font-medium mb-10 h-6">
            @if (progress() < 100) {
              <span class="flex items-center gap-2">Распознаём предметы<span class="animate-pulse">...</span></span>
            } @else {
              <span class="text-success font-bold text-lg">Готово! {{foundItems().length}} предметов найдено</span>
            }
          </p>

          <!-- Live Item Feed -->
          <div class="w-full bg-surface rounded-[32px] p-6 shadow-soft min-h-[300px]">
            <div class="flex items-center justify-between mb-4">
              <h3 class="font-bold text-text-primary">Найдено: {{foundItems().length}} предметов</h3>
            </div>

            <div class="flex flex-col gap-3">
              @for (item of foundItems(); track item.id) {
                <div class="flex items-center gap-4 p-3 rounded-[20px] bg-cream/50 animate-in slide-in-from-bottom-2 fade-in duration-300">
                  <div class="w-12 h-12 squircle flex items-center justify-center text-white font-bold shrink-0"
                       [ngStyle]="{'background-color': item.color}">
                    {{item.name.charAt(0)}}
                  </div>
                  <div class="flex-1">
                    <div class="font-semibold text-text-primary">{{item.name}}</div>
                    <div class="text-xs font-mono text-text-secondary">{{item.confidence}}% уверенность</div>
                  </div>
                  <div class="w-8 h-8 rounded-full bg-success/10 text-success flex items-center justify-center">
                    <mat-icon class="text-[16px] w-4 h-4">check</mat-icon>
                  </div>
                </div>
              }
            </div>
          </div>

          @if (progress() === 100) {
            <button class="mt-8 rounded-[20px] bg-accent text-white px-8 py-4 font-bold text-lg flex items-center gap-2 hover:scale-[0.97] transition-transform shadow-soft animate-in slide-in-from-bottom-4 fade-in">
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
export class ScanComponent {
  state = signal<'upload' | 'processing'>('upload');
  progress = signal(0);
  foundItems = signal<any[]>([]);

  allItems = [
    { id: '1', name: 'Крестовая отвёртка PH2', confidence: 97, color: '#FF6B7A' },
    { id: '2', name: 'Разводной ключ 250мм', confidence: 94, color: '#FF9F43' },
    { id: '3', name: 'Мультиметр DT830B', confidence: 91, color: '#5BB8FF' },
  ];

  startScan() {
    this.state.set('processing');
    this.progress.set(0);
    this.foundItems.set([]);

    let currentProgress = 0;
    let itemIndex = 0;

    const interval = setInterval(() => {
      currentProgress += Math.floor(Math.random() * 15) + 5;
      
      if (currentProgress >= 100) {
        currentProgress = 100;
        clearInterval(interval);
      }
      
      this.progress.set(currentProgress);

      // Add items progressively
      if (currentProgress > 30 && itemIndex === 0) {
        this.foundItems.update(items => [...items, this.allItems[0]]);
        itemIndex++;
      } else if (currentProgress > 60 && itemIndex === 1) {
        this.foundItems.update(items => [...items, this.allItems[1]]);
        itemIndex++;
      } else if (currentProgress >= 90 && itemIndex === 2) {
        this.foundItems.update(items => [...items, this.allItems[2]]);
        itemIndex++;
      }

    }, 400);
  }
}
