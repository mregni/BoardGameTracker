import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cx } from 'class-variance-authority';
import { Text } from '@radix-ui/themes';

import { MenuItem } from '@/models';
import { usePage } from '@/hooks/usePage';

interface Props {
  item: MenuItem;
  count: number | undefined;
}

export const BgtMenuItem = (props: Props) => {
  const { item, count } = props;
  const { activePage } = usePage();
  const { t } = useTranslation();

  return (
    <Link to={item.path}>
      <div
        className={cx(
          'flex w-full p-2 text-white cursor-pointer border border-transparent items-center my-1 hover:bg-primary-dark md:rounded-lg font-semibold justify-between px-2',
          activePage == item.path && 'bg-primary-dark border border-primary'
        )}
      >
        <div className="flex items-center gap-2">
          {item.icon}
          <Text as="span" size="3">
            {t(item.menuLabel)}
          </Text>
        </div>
        {count !== undefined && <div className="py-1 px-3 flex items-center justify-center text-xs">{count}</div>}
      </div>
    </Link>
  );
};
