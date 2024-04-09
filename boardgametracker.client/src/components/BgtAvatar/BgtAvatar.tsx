import clsx from 'clsx';

import {Text} from '@radix-ui/themes';

import {Player} from '../../models';
import {stringToColor} from '../../utils/stringToColor';

interface Props {
  player: Player | null;
  onClick?: () => void;
}

export const BgtAvatar = (props: Props) => {
  const { player, onClick } = props;

  if (player === null) return null;

  return (
    <div className='group flex relative'>
      {
        player.image && (
          <img
            className={
              clsx(
                'rounded-md shadow-gray-800 shadow-md h-7 w-7',
                onClick && 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer'
              )
            }
            onClick={onClick}
            src={`/${player.image}`}
          />
        )
      }
      {
        !player.image && (
          <div style={{ backgroundColor: stringToColor(player.name) }}
            className={
              clsx(
                'rounded-md shadow-gray-800 shadow-md h-7 w-7 flex justify-center items-center',
                onClick && 'hover:scale-95 hover:shadow-black hover:shadow-lg hover:cursor-pointer'
              )
            }>
            <Text size="2">{player.name[0]}</Text>
          </div>
        )
      }
      <span className="group-hover:opacity-100 group-hover:block bg-black py-1 px-1.5 text-sm text-white rounded-md absolute hidden left-1/2 z-50
    -translate-x-1/2 -translate-y-full -top-2 opacity-0 mx-auto font-sans font-normal focus:outline-none shadow-black shadow-md">{player.name}</span>
    </div>
  )
}