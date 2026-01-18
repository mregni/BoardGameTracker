import { describe, it, expect, vi } from 'vitest';
import { render, screen } from '@/test/test-utils';

import { BgtDataGuard } from './BgtDataGuard';

vi.mock('../BgtLoadingSpinner/BgtLoadingSpinner', () => ({
  BgtLoadingSpinner: () => <div data-testid="loading-spinner">Loading...</div>,
}));

describe('BgtDataGuard', () => {
  describe('Loading State', () => {
    it('should render fallback when isLoading is true', () => {
      render(
        <BgtDataGuard isLoading={true} data={{ name: 'John' }}>
          {({ name }) => <div>Hello {name}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
      expect(screen.queryByText('Hello John')).not.toBeInTheDocument();
    });

    it('should render default loading spinner fallback', () => {
      render(
        <BgtDataGuard isLoading={true} data={{ value: 42 }}>
          {({ value }) => <div>Value: {value}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
    });

    it('should render custom fallback when provided', () => {
      render(
        <BgtDataGuard
          isLoading={true}
          data={{ name: 'John' }}
          fallback={<div data-testid="custom-fallback">Custom Loading</div>}
        >
          {({ name }) => <div>Hello {name}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByTestId('custom-fallback')).toBeInTheDocument();
      expect(screen.queryByTestId('loading-spinner')).not.toBeInTheDocument();
    });
  });

  describe('Undefined Data', () => {
    it('should render fallback when any data property is undefined', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ name: 'John', age: undefined }}>
          {({ name, age }) => (
            <div>
              {name} is {age} years old
            </div>
          )}
        </BgtDataGuard>
      );

      expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
    });

    it('should render fallback when all data properties are undefined', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ name: undefined, age: undefined }}>
          {({ name, age }) => (
            <div>
              {name} is {age} years old
            </div>
          )}
        </BgtDataGuard>
      );

      expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
    });

    it('should render custom fallback when data is undefined', () => {
      render(
        <BgtDataGuard
          isLoading={false}
          data={{ value: undefined }}
          fallback={<div data-testid="no-data">No data available</div>}
        >
          {({ value }) => <div>Value: {value}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByTestId('no-data')).toBeInTheDocument();
    });
  });

  describe('Valid Data', () => {
    it('should render children when all data is defined', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ name: 'John', age: 30 }}>
          {({ name, age }) => (
            <div>
              {name} is {age} years old
            </div>
          )}
        </BgtDataGuard>
      );

      expect(screen.getByText('John is 30 years old')).toBeInTheDocument();
      expect(screen.queryByTestId('loading-spinner')).not.toBeInTheDocument();
    });

    it('should pass data to children function with correct types', () => {
      const childFn = vi.fn().mockReturnValue(<div>Content</div>);

      render(
        <BgtDataGuard isLoading={false} data={{ name: 'Alice', count: 5 }}>
          {childFn}
        </BgtDataGuard>
      );

      expect(childFn).toHaveBeenCalledWith({ name: 'Alice', count: 5 });
    });

    it('should handle single data property', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ value: 'test' }}>
          {({ value }) => <div>Value: {value}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByText('Value: test')).toBeInTheDocument();
    });

    it('should handle multiple data properties', () => {
      render(
        <BgtDataGuard
          isLoading={false}
          data={{
            player: { name: 'John' },
            statistics: { wins: 10 },
            badges: ['gold', 'silver'],
            settings: { theme: 'dark' },
          }}
        >
          {({ player, statistics, badges, settings }) => (
            <div>
              <span>{player.name}</span>
              <span>{statistics.wins} wins</span>
              <span>{badges.length} badges</span>
              <span>{settings.theme} theme</span>
            </div>
          )}
        </BgtDataGuard>
      );

      expect(screen.getByText('John')).toBeInTheDocument();
      expect(screen.getByText('10 wins')).toBeInTheDocument();
      expect(screen.getByText('2 badges')).toBeInTheDocument();
      expect(screen.getByText('dark theme')).toBeInTheDocument();
    });
  });

  describe('Falsy but Defined Values', () => {
    it('should render children when data is null (not undefined)', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ value: null }}>
          {({ value }) => <div>Value is {value === null ? 'null' : 'not null'}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByText('Value is null')).toBeInTheDocument();
    });

    it('should render children when data is empty string', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ text: '' }}>
          {({ text }) => <div>Text: "{text}"</div>}
        </BgtDataGuard>
      );

      expect(screen.getByText('Text: ""')).toBeInTheDocument();
    });

    it('should render children when data is zero', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ count: 0 }}>
          {({ count }) => <div>Count: {count}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByText('Count: 0')).toBeInTheDocument();
    });

    it('should render children when data is false', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ active: false }}>
          {({ active }) => <div>Active: {active ? 'yes' : 'no'}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByText('Active: no')).toBeInTheDocument();
    });

    it('should render children when data is empty array', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ items: [] }}>
          {({ items }) => <div>Items: {items.length}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByText('Items: 0')).toBeInTheDocument();
    });

    it('should render children when data is empty object', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ config: {} }}>
          {({ config }) => <div>Config: {Object.keys(config).length} keys</div>}
        </BgtDataGuard>
      );

      expect(screen.getByText('Config: 0 keys')).toBeInTheDocument();
    });
  });

  describe('Priority of Checks', () => {
    it('should check isLoading before data', () => {
      render(
        <BgtDataGuard isLoading={true} data={{ value: undefined }}>
          {({ value }) => <div>Value: {value}</div>}
        </BgtDataGuard>
      );

      expect(screen.getByTestId('loading-spinner')).toBeInTheDocument();
    });
  });

  describe('Empty Data Object', () => {
    it('should render children with empty data object', () => {
      render(
        <BgtDataGuard isLoading={false} data={{}}>
          {() => <div>No data props</div>}
        </BgtDataGuard>
      );

      expect(screen.getByText('No data props')).toBeInTheDocument();
    });
  });

  describe('Complex Children', () => {
    it('should render complex component trees', () => {
      render(
        <BgtDataGuard isLoading={false} data={{ user: { id: 1, name: 'John' } }}>
          {({ user }) => (
            <div data-testid="user-card">
              <header>
                <h1>{user.name}</h1>
              </header>
              <main>
                <p>User ID: {user.id}</p>
              </main>
            </div>
          )}
        </BgtDataGuard>
      );

      expect(screen.getByTestId('user-card')).toBeInTheDocument();
      expect(screen.getByRole('heading', { name: 'John' })).toBeInTheDocument();
      expect(screen.getByText('User ID: 1')).toBeInTheDocument();
    });
  });
});
