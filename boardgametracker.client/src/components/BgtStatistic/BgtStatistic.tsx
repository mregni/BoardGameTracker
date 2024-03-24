import {Text} from '@radix-ui/themes';

interface Props {
  title: string;
  content: string | number | null | undefined;
  suffix?: string;
}

export const BgtStatistic = (props: Props) => {
  const { title, content, suffix } = props;
  if (content === null || content === undefined) return null;

  return (
    <div className='flex flex-col justify-center gap-1 pl-3'>
      <Text size="1" className='uppercase'>{title}</Text>
      <div className='text-xl font-bold'>{content}{suffix && <span className='text-sm'>&nbsp;{suffix}</span>}</div>
    </div>
  )
}