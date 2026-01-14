import { useTranslation } from 'react-i18next';
import { Link } from '@tanstack/react-router';
import { Text } from '@radix-ui/themes';

import { MenuItem } from '@/models';

interface Props {
  item: MenuItem;
  count: number | undefined;
}

export const BgtMenuItem = (props: Props) => {
  const { item, count } = props;
  const { t } = useTranslation();

  const Icon = item.icon;

  return (
    <Link
      to={item.path}
      activeProps={{ className: 'bg-primary/60 border border-primary' }}
      className="flex w-full p-2 text-white cursor-pointer border border-transparent items-center my-1 hover:bg-primary/60 md:rounded-lg font-semibold justify-between px-2"
    >
      <div className="flex items-center gap-2">
        <Icon className="size-5" />
        <Text as="span" size="3">
          {t(item.menuLabel)}
        </Text>
      </div>
      {count !== undefined && count > 0 && (
        <div className="py-1 px-3 flex items-center justify-center text-xs">{count}</div>
      )}
    </Link>
  );
};
