import {useParams} from 'react-router-dom';

import {Text} from '@radix-ui/themes';

import {BgtLinkedText} from '../../../components/BgtLinkedText/BgtLinkedText';
import {useGame} from '../../../hooks/useGame';

export const GameCategories = () => {
  const { id } = useParams();
  const { game } = useGame(id);

  if (game === undefined) return null;
  
  return (
    <div className='flex flex-col gap-3 divide-y divide-sky-600'>
      <Text size="3">
        {game.categories.map(x => <BgtLinkedText key={x.id} value={x.name} link={x.id} />)}
      </Text>
    </div>
  )
}