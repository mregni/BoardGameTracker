import {format} from 'date-fns';

import {useSettings} from '../../hooks/useSettings';

interface Props {
  dateTime: Date;
}

export const BgtDateTimeCell = (props: Props) => {
  const { dateTime } = props;
  const { settings } = useSettings();

  if(settings === undefined) return null;

  return (
    <div className='flex flex-col justify-center flex-none'>
      <div className='font-bold'>{format(dateTime, settings?.dateFormat ?? '')}</div>
      <div className='text-xs'>{format(dateTime, settings?.timeFormat ?? '')}</div>
    </div>
  )
}