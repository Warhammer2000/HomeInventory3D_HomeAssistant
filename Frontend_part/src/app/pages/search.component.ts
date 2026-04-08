import {ChangeDetectionStrategy, Component, signal} from '@angular/core';
import {NgStyle} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [NgStyle, MatIconModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-6 md:p-10 max-w-4xl mx-auto animate-in fade-in slide-in-from-bottom-4 duration-500">
      <!-- Search Input -->
      <div class="relative mb-10 group">
        <div class="absolute inset-y-0 left-6 flex items-center pointer-events-none text-text-secondary group-focus-within:text-accent transition-colors">
          <mat-icon class="text-3xl w-8 h-8 flex items-center justify-center">search</mat-icon>
        </div>
        <input type="text" 
               [value]="searchQuery()"
               (input)="onSearch($event)"
               class="w-full h-20 pl-16 pr-6 bg-surface rounded-[24px] text-xl font-heading font-bold text-text-primary placeholder:text-text-secondary/50 focus:outline-none focus:ring-4 focus:ring-accent/10 shadow-soft transition-all"
               placeholder="Найти предмет...">
      </div>

      @if (searchQuery().length === 0) {
        <!-- Empty State / Recent -->
        <div class="animate-in fade-in duration-300">
          <h3 class="text-text-secondary font-bold mb-4">Недавние запросы</h3>
          <div class="flex flex-wrap gap-2 mb-12">
            @for (term of recentSearches; track term) {
              <button class="px-4 py-2 bg-surface rounded-full text-sm font-semibold text-text-primary hover:bg-surface-secondary hover:text-accent transition-colors shadow-sm"
                      (click)="setSearch(term)">
                {{term}}
              </button>
            }
          </div>

          <div class="flex flex-col items-center justify-center text-center mt-20 opacity-60">
            <div class="w-32 h-32 squircle bg-surface-secondary text-accent flex items-center justify-center mb-6">
              <mat-icon class="text-6xl w-16 h-16 flex items-center justify-center">search</mat-icon>
            </div>
            <p class="text-text-secondary font-medium">Попробуйте: отвёртка, USB, ключ</p>
          </div>
        </div>
      } @else {
        <!-- Results -->
        <div class="animate-in slide-in-from-bottom-4 fade-in duration-300">
          @for (group of results(); track group.containerName) {
            <div class="mb-8">
              <div class="flex items-center gap-2 mb-4 px-2">
                <div class="w-3 h-3 rounded-full" [ngStyle]="{'background-color': group.color}"></div>
                <h3 class="font-bold text-text-secondary">{{group.containerName}}</h3>
                <span class="text-xs text-text-secondary/60 ml-auto">{{group.location}}</span>
              </div>

              <div class="flex flex-col gap-3">
                @for (item of group.items; track item.id) {
                  <div class="bg-surface rounded-[20px] p-4 flex items-center gap-4 shadow-sm hover:shadow-soft transition-shadow cursor-pointer">
                    <div class="w-12 h-12 squircle flex items-center justify-center text-white font-bold shrink-0"
                         [ngStyle]="{'background-color': group.color}">
                      {{item.name.charAt(0)}}
                    </div>
                    
                    <div class="flex-1 min-w-0">
                      <!-- Highlighted name -->
                      <h4 class="font-semibold text-text-primary truncate mb-1">
                        <span class="bg-sunflower/30 px-1 rounded">{{item.name.substring(0, searchQuery().length)}}</span>{{item.name.substring(searchQuery().length)}}
                      </h4>
                      <div class="flex flex-wrap gap-1.5">
                        @for (tag of item.tags; track tag) {
                          <span class="px-2 py-0.5 text-[10px] font-bold rounded-full"
                                [ngStyle]="{'background-color': group.color + '20', 'color': group.color}">
                            {{tag}}
                          </span>
                        }
                      </div>
                    </div>
                    
                    <mat-icon class="text-text-secondary">chevron_right</mat-icon>
                  </div>
                }
              </div>
            </div>
          }
          
          @if (results().length === 0) {
            <div class="flex flex-col items-center justify-center text-center mt-20">
              <div class="w-32 h-32 squircle bg-surface flex items-center justify-center mb-6 shadow-sm">
                <mat-icon class="text-6xl w-16 h-16 flex items-center justify-center text-text-secondary">question_mark</mat-icon>
              </div>
              <h3 class="text-xl font-bold text-text-primary mb-2">Ничего не нашлось</h3>
              <p class="text-text-secondary font-medium">Попробуйте изменить запрос</p>
            </div>
          }
        </div>
      }
    </div>
  `
})
export class SearchComponent {
  searchQuery = signal('');
  recentSearches = ['отвёртка', 'кабель USB', 'ключ на 10', 'изолента'];

  allData = [
    {
      containerName: 'Ящик с инструментами',
      location: 'Гараж, полка 2',
      color: '#FF6B7A',
      items: [
        { id: '1', name: 'Крестовая отвёртка PH2', tags: ['инструмент', 'отвёртка'] },
        { id: '2', name: 'Разводной ключ 250мм', tags: ['инструмент', 'ключ'] },
      ]
    },
    {
      containerName: 'Коробка электроники',
      location: 'Кабинет, шкаф',
      color: '#5BB8FF',
      items: [
        { id: '3', name: 'USB-C переходник', tags: ['электроника', 'кабель'] },
      ]
    }
  ];

  results = signal<any[]>([]);

  onSearch(event: Event) {
    const query = (event.target as HTMLInputElement).value;
    this.setSearch(query);
  }

  setSearch(query: string) {
    this.searchQuery.set(query);
    
    if (!query) {
      this.results.set([]);
      return;
    }

    const lowerQuery = query.toLowerCase();
    const filtered = this.allData.map(group => {
      const matchedItems = group.items.filter(item => 
        item.name.toLowerCase().includes(lowerQuery) || 
        item.tags.some(t => t.toLowerCase().includes(lowerQuery))
      );
      return { ...group, items: matchedItems };
    }).filter(group => group.items.length > 0);

    this.results.set(filtered);
  }
}
