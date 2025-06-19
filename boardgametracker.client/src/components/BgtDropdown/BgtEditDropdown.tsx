import { useTranslation } from 'react-i18next';
import { ComponentPropsWithoutRef } from 'react';
import * as DropdownMenu from '@radix-ui/react-dropdown-menu';

import { BgtIcon } from '../BgtIcon/BgtIcon';

import TrashIcon from '@/assets/icons/trash.svg?react';
import PencilIcon from '@/assets/icons/pencil.svg?react';
import DotsHorizontal from '@/assets/icons/dots-horizontal.svg?react';

interface Props extends ComponentPropsWithoutRef<'div'> {
  onDelete: () => void;
  onEdit: () => void;
}

export const BgtEditDropdown = (props: Props) => {
  const { className, onDelete, onEdit } = props;
  const { t } = useTranslation();

  return (
    <div className={className}>
      <DropdownMenu.Root>
        <DropdownMenu.Trigger className="focus-visible:outline-none flex items-end">
          <BgtIcon icon={<DotsHorizontal className="size-5" />} className="size-5" />
        </DropdownMenu.Trigger>

        <DropdownMenu.Portal>
          <DropdownMenu.Content
            className="bg-card-black border border-card-border p-2 rounded-md data-[side=top]:animate-slideDownAndFade data-[side=right]:animate-slideLeftAndFade data-[side=bottom]:animate-slideUpAndFade data-[side=left]:animate-slideRightAndFade"
            sideOffset={5}
          >
            <DropdownMenu.Item
              onClick={() => {
                onEdit();
              }}
              className="leading-none rounded-md flex items-center gap-3 p-2 select-none outline-none data-[highlighted]:bg-primary"
            >
              <PencilIcon className="size-5" /> <span className="first-letter:uppercase">{t('common.edit')}</span>
            </DropdownMenu.Item>
            <DropdownMenu.Item
              onClick={() => {
                onDelete();
              }}
              className="leading-none rounded-[3px] flex items-center gap-3 p-2 select-none outline-none data-[highlighted]:bg-primary data-[highlighted]:text-red-500 text-red-500"
            >
              <TrashIcon className="size-5" />{' '}
              <span className="first-letter:uppercase">{t('common.delete.button')}</span>
            </DropdownMenu.Item>
          </DropdownMenu.Content>
        </DropdownMenu.Portal>
      </DropdownMenu.Root>
    </div>
  );
};
