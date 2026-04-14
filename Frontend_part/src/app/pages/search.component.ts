import {ChangeDetectionStrategy, Component, inject, signal, AfterViewInit, OnDestroy, ElementRef} from '@angular/core';
import {NgStyle} from '@angular/common';
import {MatIconModule} from '@angular/material/icon';
import {RouterLink} from '@angular/router';
import {ItemService} from '../services/item.service';
import gsap from 'gsap';
import {ItemDto} from '../models/item.model';
import {Subject} from 'rxjs';
import {debounceTime, distinctUntilChanged, switchMap, of, catchError} from 'rxjs';

interface SearchGroup {
  containerId: string;
  containerName: string;
  location: string;
  color: string;
  items: ItemDto[];
}

const COLORS = ['#FF6B7A', '#5BB8FF', '#51E2A2', '#FFD43B', '#9B8AFB', '#FF9F43'];

@Component({
  selector: 'app-search',
  standalone: true,
  imports: [NgStyle, MatIconModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="p-6 md:p-10 max-w-4xl mx-auto search-page">
      <!-- Search Input -->
      <div class="search-box relative mb-10 group">
        <div class="absolute inset-y-0 left-6 flex items-center pointer-events-none text-text-secondary group-focus-within:text-accent transition-colors">
          <mat-icon class="text-3xl w-8 h-8 flex items-center justify-center">search</mat-icon>
        </div>
        <input type="text"
               [value]="searchQuery()"
               (input)="onSearch($event)"
               class="w-full h-20 pl-16 pr-6 bg-surface rounded-[24px] text-xl font-heading font-bold text-text-primary placeholder:text-text-secondary/50 focus:outline-none focus:ring-4 focus:ring-accent/10 shadow-soft transition-all"
               placeholder="Найти предмет...">
      </div>

      @if (loading()) {
        <div class="flex justify-center py-10">
          <div class="w-8 h-8 border-4 border-accent/20 border-t-accent rounded-full animate-spin"></div>
        </div>
      } @else if (searchQuery().length === 0) {
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
        <div class="animate-in slide-in-from-bottom-4 fade-in duration-300">
          @for (group of results(); track group.containerId) {
            <div class="mb-8">
              <a [routerLink]="['/container', group.containerId]"
                 class="flex items-center gap-2 mb-4 px-2 hover:opacity-80 transition-opacity">
                <div class="w-3 h-3 rounded-full" [ngStyle]="{'background-color': group.color}"></div>
                <h3 class="font-bold text-text-secondary">{{group.containerName}}</h3>
                <span class="text-xs text-text-secondary/60 ml-auto">{{group.location}}</span>
              </a>

              <div class="flex flex-col gap-3">
                @for (item of group.items; track item.id) {
                  <div class="search-result bg-surface rounded-[20px] p-4 flex items-center gap-4 shadow-sm hover:shadow-soft transition-shadow cursor-pointer">
                    <div class="w-12 h-12 squircle flex items-center justify-center text-white font-bold shrink-0"
                         [ngStyle]="{'background-color': group.color}">
                      {{item.name.charAt(0)}}
                    </div>

                    <div class="flex-1 min-w-0">
                      <h4 class="font-semibold text-text-primary truncate mb-1">{{item.name}}</h4>
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

          @if (results().length === 0 && searchQuery().length > 0) {
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
export class SearchComponent implements AfterViewInit, OnDestroy {
  private itemService = inject(ItemService);
  private el = inject(ElementRef);
  private ctx!: gsap.Context;

  searchQuery = signal('');
  results = signal<SearchGroup[]>([]);
  loading = signal(false);
  recentSearches = ['отвёртка', 'кабель USB', 'ключ', 'изолента'];

  private searchSubject = new Subject<string>();
  private colorMap = new Map<string, string>();
  private colorIndex = 0;

  constructor() {
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      switchMap(query => {
        if (!query || query.length < 2) {
          this.results.set([]);
          this.loading.set(false);
          return of([]);
        }
        this.loading.set(true);
        return this.itemService.search(query).pipe(
          catchError(() => {
            this.loading.set(false);
            return of([]);
          })
        );
      })
    ).subscribe(items => {
      this.loading.set(false);
      this.groupByContainer(items);
      setTimeout(() => this.animateResults(), 50);
    });
  }

  ngAfterViewInit() {
    this.ctx = gsap.context(() => {
      // Search box — dramatic entrance
      gsap.from('.search-box', {
        y: -40, scale: 1.05, autoAlpha: 0,
        duration: 0.6, ease: 'expo.out'
      });
      // Recent searches — wave stagger
      gsap.from('.search-page button', {
        y: 15, autoAlpha: 0, duration: 0.4,
        ease: 'power3.out',
        stagger: { each: 0.05, from: 'random' },
        delay: 0.3
      });
    }, this.el.nativeElement);
  }

  ngOnDestroy() {
    this.ctx?.revert();
  }

  private animateResults() {
    gsap.from('.search-result', {
      x: -30, autoAlpha: 0, duration: 0.4,
      ease: 'power4.out',
      stagger: 0.05
    });
  }

  onSearch(event: Event) {
    const query = (event.target as HTMLInputElement).value;
    this.searchQuery.set(query);
    this.searchSubject.next(query);
  }

  setSearch(query: string) {
    this.searchQuery.set(query);
    this.searchSubject.next(query);
  }

  private groupByContainer(items: ItemDto[]) {
    const groups = new Map<string, SearchGroup>();

    for (const item of items) {
      if (!groups.has(item.containerId)) {
        groups.set(item.containerId, {
          containerId: item.containerId,
          containerName: `Container`,
          location: '',
          color: this.getContainerColor(item.containerId),
          items: []
        });
      }
      groups.get(item.containerId)!.items.push(item);
    }

    this.results.set(Array.from(groups.values()));
  }

  private getContainerColor(containerId: string): string {
    if (!this.colorMap.has(containerId)) {
      this.colorMap.set(containerId, COLORS[this.colorIndex % COLORS.length]);
      this.colorIndex++;
    }
    return this.colorMap.get(containerId)!;
  }
}
