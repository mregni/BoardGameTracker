import { HTMLAttributes, TdHTMLAttributes, ThHTMLAttributes } from 'react';
import { cx } from 'class-variance-authority';

export const BgtTable = ({ className, ...props }: HTMLAttributes<HTMLTableElement>) => (
  <div>
    <table className={cx('min-w-full divide-y divide-gray-200 table-fixed', className)} {...props} />
  </div>
);

export const BgtTableHeader = ({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) => (
  <thead className={cx(className)} {...props} />
);

export const BgtTableHead = ({ className, ...props }: ThHTMLAttributes<HTMLTableCellElement>) => (
  <th className={cx('px-3 py-2.5 font-bold text-xs text-left text-gray-main', className)} {...props} />
);

export const BgtTableBody = ({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) => (
  <tbody className={cx('divide-y divide-gray-800 text-base', className)} {...props} />
);

export const BgtTableRow = ({ className, ...props }: HTMLAttributes<HTMLTableRowElement>) => (
  <tr className={cx('px-3', className)} {...props} />
);

export const BgtTableCell = ({ className, ...props }: TdHTMLAttributes<HTMLTableCellElement>) => (
  <td className={cx('font-bold text-sm text-gray-dark pl-3', className)} {...props} />
);

export const BgtTableCaption = ({ className, ...props }: HTMLAttributes<HTMLTableCaptionElement>) => (
  <caption className={cx(className)} {...props} />
);

export const BgtTableFooter = ({ className, ...props }: HTMLAttributes<HTMLTableSectionElement>) => (
  <tfoot className={cx(className)} {...props} />
);
