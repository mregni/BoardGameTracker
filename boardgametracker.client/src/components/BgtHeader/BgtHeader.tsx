import { IconButton, TextField } from '@radix-ui/themes';

import UserIcon from '@/assets/icons/user.svg?react';
import MagnifyingGlassIcon from '@/assets/icons/magnifying-glass.svg?react';

export const BgtHeader = () => {
  return (
    <div className="bg-card-black flex flex-row justify-end p-3 m-3 rounded gap-3">
      <div className="hidden">
        <TextField.Root type="text" placeholder="Search item (hardcoded)" className="w-64" radius="large">
          <TextField.Slot></TextField.Slot>
          <TextField.Slot>
            <IconButton size="1" variant="ghost">
              <MagnifyingGlassIcon height="16" width="16" className="mr-1" />
            </IconButton>
          </TextField.Slot>
        </TextField.Root>
      </div>
      <div className="flex justify-center flex-col mr-1">
        <UserIcon className="size-5" />
      </div>
    </div>
  );
};
