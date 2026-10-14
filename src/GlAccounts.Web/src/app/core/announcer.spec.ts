import { TestBed } from '@angular/core/testing';
import { Announcer } from './announcer';

describe('Announcer', () => {
  it('sets polite and assertive messages independently', async () => {
    const announcer = TestBed.inject(Announcer);

    announcer.announce('Account saved.');
    announcer.alert('Could not save.');
    await Promise.resolve();

    expect(announcer.polite()).toBe('Account saved.');
    expect(announcer.assertive()).toBe('Could not save.');
  });

  it('clears first so repeating a message is announced again', async () => {
    const announcer = TestBed.inject(Announcer);
    announcer.announce('Done');
    await Promise.resolve();

    announcer.announce('Done');

    expect(announcer.polite()).toBe('');
    await Promise.resolve();
    expect(announcer.polite()).toBe('Done');
  });
});
