import { describe, it, expect } from 'vitest';

import { BgtPageHeader } from './BgtPageHeader';
import { BgtPageContent } from './BgtPageContent';
import { BgtPage } from './BgtPage';

import { render, screen } from '@/test/test-utils';

describe('BgtPage', () => {
  describe('Rendering', () => {
    it('should render page header', () => {
      render(
        <BgtPage>
          <BgtPageHeader header="Test Header" />
          <BgtPageContent>
            <div>Content</div>
          </BgtPageContent>
        </BgtPage>
      );
      expect(screen.getByText('Test Header')).toBeInTheDocument();
    });

    it('should render page content', () => {
      render(
        <BgtPage>
          <BgtPageHeader header="Header" />
          <BgtPageContent>
            <div data-testid="page-content">Page Content</div>
          </BgtPageContent>
        </BgtPage>
      );
      expect(screen.getByTestId('page-content')).toBeInTheDocument();
    });

    it('should render both header and content', () => {
      render(
        <BgtPage>
          <BgtPageHeader header="My Page" />
          <BgtPageContent>
            <div>Main Content</div>
          </BgtPageContent>
        </BgtPage>
      );
      expect(screen.getByText('My Page')).toBeInTheDocument();
      expect(screen.getByText('Main Content')).toBeInTheDocument();
    });
  });

  describe('Component Order', () => {
    it('should handle header and content in any order', () => {
      render(
        <BgtPage>
          <BgtPageContent>
            <div data-testid="content">Content First</div>
          </BgtPageContent>
          <BgtPageHeader header="Header Second" />
        </BgtPage>
      );
      expect(screen.getByText('Header Second')).toBeInTheDocument();
      expect(screen.getByTestId('content')).toBeInTheDocument();
    });
  });

  describe('Edge Cases', () => {
    it('should handle only header', () => {
      render(
        <BgtPage>
          <BgtPageHeader header="Only Header" />
        </BgtPage>
      );
      expect(screen.getByText('Only Header')).toBeInTheDocument();
    });

    it('should handle only content', () => {
      render(
        <BgtPage>
          <BgtPageContent>
            <div data-testid="only-content">Only Content</div>
          </BgtPageContent>
        </BgtPage>
      );
      expect(screen.getByTestId('only-content')).toBeInTheDocument();
    });
  });
});
