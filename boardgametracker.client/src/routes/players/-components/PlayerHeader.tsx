import { BgtHeading } from '@/components/BgtHeading/BgtHeading';
import { BgtEditDeleteButtons } from '@/components/BgtButton/BgtEditDeleteButtons';

interface Props {
  playerName: string;
  onEdit: () => void;
  onDelete: () => void;
}

export const PlayerHeader = (props: Props) => {
  const { playerName, onEdit, onDelete } = props;

  return (
    <div className="flex md:flex-row flex-col justify-between">
      <div className="flex flex-col-reverse xl:flex-row gap-1">
        <BgtHeading size="8" className="shrink-0">
          {playerName}
        </BgtHeading>
      </div>
      <div className="flex gap-3 justify-between md:justify-start items-center pt-2 md:pt-0">
        <BgtEditDeleteButtons onDelete={onDelete} onEdit={onEdit} />
      </div>
    </div>
  );
};
