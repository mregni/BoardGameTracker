import { Text } from '@radix-ui/themes';

export interface Props {
  fullSize?: boolean;
}

export const BgtMenuLogo = (props: Props) => {
  const { fullSize = true } = props;

  return (
    <div className={'h-16 w-full flex gap-2 items-center justify-center'}>
      <Text size="5" className="text-purple-500 font-bold">
        Boardgame company
      </Text>
    </div>
  );
};
