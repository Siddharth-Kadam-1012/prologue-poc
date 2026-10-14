import { TestBed } from '@angular/core/testing';
import { BlockerList, blockerLabel } from './blocker-list';
import { Component } from '@angular/core';

describe('BlockerList', () => {
  it('has a readable label for every blocker kind', () => {
    expect(blockerLabel('PostedTransactions')).toBe('posted transaction(s)');
    expect(blockerLabel('UnpostedTransactions')).toBe('unposted transaction(s)');
    expect(blockerLabel('Budgets')).toBe('budget(s)');
    expect(blockerLabel('Allocations')).toBe('allocation(s)');
    expect(blockerLabel('GroupMemberships')).toBe('account group membership(s)');
  });

  it('AC-GLA-040 renders one item per blocker with its count', async () => {
    @Component({ imports: [BlockerList], template: `<app-blocker-list [blockers]="blockers" />` })
    class Host {
      blockers = [{ kind: 'Budgets' as const, count: 2 }];
    }
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();

    const items = (fixture.nativeElement as HTMLElement).querySelectorAll('li');
    expect(items.length).toBe(1);
    expect(items[0].textContent?.trim()).toBe('2 budget(s)');
  });
});
