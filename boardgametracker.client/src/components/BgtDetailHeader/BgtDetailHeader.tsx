import {ReactNode} from 'react';
import {useNavigate} from 'react-router-dom';

import {ArrowLeftCircleIcon} from '@heroicons/react/24/outline';
import {Button, Text} from '@radix-ui/themes';

import {BgtIcon} from '../BgtIcon/BgtIcon';
import {BgtImageFallback} from '../BgtImageCard/BgtImageFallback';

interface Props {
  title: string;
  imageSrc?: string;
  navigateBackUrl: string;
  actions: ReactNode;
}

export const BgtDetailHeader = (props: Props) => {
  const { title, imageSrc, navigateBackUrl, actions } = props;
  const navigate = useNavigate();

  return (
    <>
      <div className='absolute z-50 top-18 md:top-4'>
        <Button size="2" variant="soft" className='hover:cursor-pointer' onClick={() => navigate(navigateBackUrl)}>
          <BgtIcon icon={<ArrowLeftCircleIcon />} size={22} />
        </Button>
      </div>
      <div className='flex items-center justify-start gap-3 flex-col'>
        <div className='max-w-24 md:max-w-56'>
          { imageSrc && <img src={imageSrc} className='rounded-sm object-fill md:border-orange-600 md:border-2 w-60 mt-4' />}
          <BgtImageFallback display={!imageSrc} title={title} />
        </div>
        <div className='flex flex-col gap-3 items-center'>
          <Text size="8" weight="bold" align="center">{title}</Text>
        </div>
        <div className='flex flex-row flex-wrap justify-center md:justify-end items-end gap-3 flex-grow h-full'>
          {actions}
        </div>
      </div>
    </>
  )
}