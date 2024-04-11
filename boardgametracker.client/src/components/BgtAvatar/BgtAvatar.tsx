import clsx from 'clsx';

import {Text} from '@radix-ui/themes';

import {stringToColor} from '../../utils/stringToColor';

interface Props {
  title: string | undefined;
  image: string | undefined;
  onClick?: () => void;
}

export const BgtAvatar = (props: Props) => {
  const { title, image, onClick } = props;

  if (title === undefined) return null;
  return (
    <div className='group flex relative min-w-7'>
      {
        image && (
          <img
            className={
              clsx(
                'rounded-md shadow-gray-800 shadow-md h-7 w-7',
                onClick && 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer'
              )
            }
            onClick={onClick}
            src={image}
          />
        )
      }
      {
        !image && (
          <div style={{ backgroundColor: stringToColor(title) }}
            onClick={onClick}
            className={
              clsx(
                'rounded-md shadow-gray-800 shadow-md h-7 w-7 flex justify-center items-center',
                onClick && 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer'
              )
            }>
            <Text size="2">{title[0]}</Text>
          </div>
        )
      }
      <span className="group-hover:opacity-100 group-hover:block bg-black py-1 px-1.5 text-sm text-white rounded-md absolute hidden left-1/2 z-50
    -translate-x-1/2 -translate-y-full -top-2 opacity-0 mx-auto font-sans font-normal focus:outline-none shadow-black shadow-md">{title}</span>
    </div>
  )
}