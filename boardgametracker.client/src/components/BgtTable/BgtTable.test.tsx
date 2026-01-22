import { describe, it, expect } from 'vitest';

import {
  BgtTable,
  BgtTableHeader,
  BgtTableHead,
  BgtTableBody,
  BgtTableRow,
  BgtTableCell,
  BgtTableCaption,
  BgtTableFooter,
} from './BgtTable';

import { render, screen } from '@/test/test-utils';

describe('BgtTable Components', () => {
  describe('BgtTable', () => {
    it('should render table element', () => {
      render(
        <BgtTable>
          <tbody>
            <tr>
              <td>Content</td>
            </tr>
          </tbody>
        </BgtTable>
      );
      expect(screen.getByRole('table')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      render(
        <BgtTable className="custom-table">
          <tbody>
            <tr>
              <td>Content</td>
            </tr>
          </tbody>
        </BgtTable>
      );
      const table = screen.getByRole('table');
      expect(table).toHaveClass('custom-table');
    });
  });

  describe('BgtTableHeader', () => {
    it('should render thead element', () => {
      render(
        <table>
          <BgtTableHeader>
            <tr>
              <th>Header</th>
            </tr>
          </BgtTableHeader>
        </table>
      );
      expect(screen.getByRole('rowgroup')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      render(
        <table>
          <BgtTableHeader className="custom-header">
            <tr>
              <th>Header</th>
            </tr>
          </BgtTableHeader>
        </table>
      );
      const header = screen.getByRole('rowgroup');
      expect(header).toHaveClass('custom-header');
    });
  });

  describe('BgtTableHead', () => {
    it('should render th element', () => {
      render(
        <table>
          <thead>
            <tr>
              <BgtTableHead>Column</BgtTableHead>
            </tr>
          </thead>
        </table>
      );
      expect(screen.getByRole('columnheader')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      render(
        <table>
          <thead>
            <tr>
              <BgtTableHead className="w-1/2">Column</BgtTableHead>
            </tr>
          </thead>
        </table>
      );
      const th = screen.getByRole('columnheader');
      expect(th).toHaveClass('w-1/2');
    });
  });

  describe('BgtTableBody', () => {
    it('should render tbody element', () => {
      render(
        <table>
          <BgtTableBody>
            <tr>
              <td>Cell</td>
            </tr>
          </BgtTableBody>
        </table>
      );
      expect(screen.getByRole('rowgroup')).toBeInTheDocument();
    });
  });

  describe('BgtTableRow', () => {
    it('should render tr element', () => {
      render(
        <table>
          <tbody>
            <BgtTableRow>
              <td>Cell</td>
            </BgtTableRow>
          </tbody>
        </table>
      );
      expect(screen.getByRole('row')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      render(
        <table>
          <tbody>
            <BgtTableRow className="highlighted">
              <td>Cell</td>
            </BgtTableRow>
          </tbody>
        </table>
      );
      const row = screen.getByRole('row');
      expect(row).toHaveClass('highlighted');
    });
  });

  describe('BgtTableCell', () => {
    it('should render td element', () => {
      render(
        <table>
          <tbody>
            <tr>
              <BgtTableCell>Cell Content</BgtTableCell>
            </tr>
          </tbody>
        </table>
      );
      expect(screen.getByRole('cell')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      render(
        <table>
          <tbody>
            <tr>
              <BgtTableCell className="text-right">Cell Content</BgtTableCell>
            </tr>
          </tbody>
        </table>
      );
      const cell = screen.getByRole('cell');
      expect(cell).toHaveClass('text-right');
    });
  });

  describe('BgtTableCaption', () => {
    it('should render caption element', () => {
      render(
        <table>
          <BgtTableCaption>Table Caption</BgtTableCaption>
          <tbody>
            <tr>
              <td>Cell</td>
            </tr>
          </tbody>
        </table>
      );
      expect(screen.getByText('Table Caption')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      render(
        <table>
          <BgtTableCaption className="caption-bottom">Caption</BgtTableCaption>
          <tbody>
            <tr>
              <td>Cell</td>
            </tr>
          </tbody>
        </table>
      );
      const caption = screen.getByText('Caption');
      expect(caption).toHaveClass('caption-bottom');
    });
  });

  describe('BgtTableFooter', () => {
    it('should render tfoot element', () => {
      render(
        <table>
          <tbody>
            <tr>
              <td>Cell</td>
            </tr>
          </tbody>
          <BgtTableFooter>
            <tr>
              <td>Footer</td>
            </tr>
          </BgtTableFooter>
        </table>
      );
      expect(screen.getByText('Footer')).toBeInTheDocument();
    });

    it('should apply custom className', () => {
      render(
        <table>
          <tbody>
            <tr>
              <td>Cell</td>
            </tr>
          </tbody>
          <BgtTableFooter className="sticky-footer" data-testid="footer">
            <tr>
              <td>Footer</td>
            </tr>
          </BgtTableFooter>
        </table>
      );
      const footer = screen.getByTestId('footer');
      expect(footer).toHaveClass('sticky-footer');
    });
  });

  describe('Complete Table', () => {
    it('should render a complete table structure', () => {
      render(
        <BgtTable>
          <BgtTableHeader>
            <BgtTableRow>
              <BgtTableHead>Name</BgtTableHead>
              <BgtTableHead>Score</BgtTableHead>
            </BgtTableRow>
          </BgtTableHeader>
          <BgtTableBody>
            <BgtTableRow>
              <BgtTableCell>John</BgtTableCell>
              <BgtTableCell>100</BgtTableCell>
            </BgtTableRow>
            <BgtTableRow>
              <BgtTableCell>Jane</BgtTableCell>
              <BgtTableCell>95</BgtTableCell>
            </BgtTableRow>
          </BgtTableBody>
        </BgtTable>
      );

      expect(screen.getByRole('table')).toBeInTheDocument();
      expect(screen.getAllByRole('columnheader')).toHaveLength(2);
      expect(screen.getAllByRole('row')).toHaveLength(3); // 1 header + 2 body rows
      expect(screen.getByText('John')).toBeInTheDocument();
      expect(screen.getByText('95')).toBeInTheDocument();
    });
  });
});
