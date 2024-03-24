import {BgtIcon} from '../BgtIcon/BgtIcon';

export interface Props {
  icon: React.ReactNode
  onClick: () => void;
}
export const BgtBottomButton = (props: Props) => {
  const { icon, onClick } = props;
  return (
    <div className="grow cursor-pointer text-white pt-5 pb-3 hover:bg-sky-800 hover:rounded-md" onClick={onClick} >
      <div className='flex flex-row justify-center'>
        <BgtIcon icon={icon} />
      </div>
    </div>
  )
}