import { IconButton, TextField } from '@radix-ui/themes';
import { MagnifyingGlassIcon, UserCircleIcon } from '@heroicons/react/24/solid';

export const BgtHeader = () => {
  return (
    <div className="bg-card-black flex flex-row justify-between p-3 m-3 rounded hidden">
      <div>
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
        <UserCircleIcon height="24" width="24" />
      </div>
    </div>
  );
};
