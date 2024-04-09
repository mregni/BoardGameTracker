import clsx from 'clsx';

import {Text} from '@radix-ui/themes';

import {Player} from '../../models';
import {BgtAvatar} from '../BgtAvatar/BgtAvatar';

interface Props {
  title: string;
  content: string | number | null | undefined;
  suffix?: string;
  player?: Player | null;
}

export const BgtStatistic = (props: Props) => {
  const { title, content, suffix, player = null } = props;
  if (content === null || content === undefined) return null;

  return (
    <div className='flex flex-col justify-center gap-1 pl-3'>
      <Text size="1" className='uppercase'>{title}</Text>
      <div className={clsx('flex flex-row', player && 'gap-1 items-center')}>
        {player && <BgtAvatar player={player} />}
        <div className='text-xl font-bold'>{content}{suffix && <span className='text-sm'>&nbsp;{suffix}</span>}</div>
      </div>
    </div>
  )
}