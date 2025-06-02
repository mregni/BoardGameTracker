import { BgtIconButton } from '../BgtIconButton/BgtIconButton';
import { BgtNormalEditDropdown } from '../BgtDropdown/BgtEditDropdown';

import TrashIcon from '@/assets/icons/trash.svg?react';
import PencilIcon from '@/assets/icons/pencil.svg?react';

interface Props {
  onDelete: () => void;
  onEdit: () => void;
}

export const BgtEditDeleteButtons = (props: Props) => {
  const { onDelete, onEdit } = props;

  return (
    <div>
      <BgtNormalEditDropdown onDelete={onDelete} onEdit={onEdit} className="md:hidden" />
      <div className="hidden flex-row justify-end gap-2 md:flex">
        <BgtIconButton onClick={onEdit} icon={<PencilIcon />} />
        <BgtIconButton intent="danger" onClick={onDelete} icon={<TrashIcon />} />
      </div>
    </div>
  );
};
