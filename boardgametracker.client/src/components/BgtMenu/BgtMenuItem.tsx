import clsx from 'clsx';
import {useTranslation} from 'react-i18next';
import {Link} from 'react-router-dom';

import {Content, Root, TooltipProvider, Trigger} from '@radix-ui/react-tooltip';
import {Text} from '@radix-ui/themes';

import {usePage} from '../../hooks/usePage';
import {MenuItem} from '../../models';

interface Props {
  item: MenuItem
  count: number | undefined
  fullSize: boolean
}

const ItemContent = (props: Props) => {
  const { item, count, fullSize } = props;
  const { activePage } = usePage();
  const { t } = useTranslation();

  return (
    <Link to={item.path}>
      <div className={clsx(
        "h-10 flex w-full text-white cursor-pointer items-center my-2 hover:bg-sky-900",
        fullSize && "justify-between px-2",
        !fullSize && "justify-center px-4",
        activePage == item.path && "bg-sky-900"
      )
      }>
        <div className="flex items-center gap-2">
          <svg xmlns="http://www.w3.org/2000/svg" width={18} height={18} className='text-orange-600'>
            {item.icon}
          </svg>
          {fullSize && <Text as='span' size="3">{t(item.label)}</Text>}
        </div>
        {
          count && fullSize && <div className="py-1 px-3 text-white flex items-center justify-center text-xs">{count}</div>
        }
      </div >
    </Link >
  )
}

export const BgtMenuItem = (props: Props) => {
  const { item, fullSize } = props;
  const { t } = useTranslation();

  if (fullSize) return <ItemContent {...props} />

  return (
    <TooltipProvider disableHoverableContent={fullSize} delayDuration={0}>
      <Root>
        <Trigger className='h-12'>
          <ItemContent {...props} />
        </Trigger>
        <Content
          className="bg-gray-950 px-3 py-2"
          sideOffset={10}
          side="right">
          {t(item.label)}
        </Content>
      </Root>
    </TooltipProvider>
  )
}