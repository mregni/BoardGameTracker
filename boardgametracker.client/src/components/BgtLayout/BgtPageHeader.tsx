import type { ComponentType, PropsWithChildren, SVGProps } from "react";
import { useTranslation } from "react-i18next";
import ArrowLeft from "@/assets/icons/arrow-left.svg?react";
import type { Actions } from "../../models";
import BgtButton from "../BgtButton/BgtButton";
import { BgtHeading } from "../BgtHeading/BgtHeading";
import { BgtIconButton } from "../BgtIconButton/BgtIconButton";

interface Props extends PropsWithChildren {
	header?: string;
	actions?: Actions[];
	backAction?: () => void;
	backText?: string;
	icon?: ComponentType<SVGProps<SVGSVGElement>>;
}

export const BgtPageHeader = (props: Props) => {
	const { header, actions = [], backAction, backText, children = null, icon: Icon = null } = props;
	const { t } = useTranslation();

	return (
		<div className="flex flex-col gap-2">
			<div className="flex-auto flex-row flex justify-between max-lg:gap-2">
				<div className="flex flex-row gap-3 content-center items-center">
					{backAction && backText && (
						<BgtButton variant="text" onClick={backAction} className="pl-0">
							<ArrowLeft className="w-4 h-4" />
							{backText}
						</BgtButton>
					)}
					{backAction && !backText && (
						<BgtIconButton size="2" intent="header" icon={<ArrowLeft />} onClick={backAction} />
					)}
					{Icon && <Icon className="text-primary size-7" />}
					{header && <BgtHeading>{header}</BgtHeading>}
				</div>
				<div className="flex items-center flex-wrap gap-3">
					<div className="hidden md:flex">{children}</div>
					{actions.map((action, index) => (
						<BgtButton
							key={typeof action.content === "string" ? action.content : index}
							variant={action.variant}
							size="3"
							className="max-lg:py-2 max-lg:px-2 max-lg:text-xs"
							onClick={action.onClick}
						>
							{typeof action.content === "string" ? t(action.content) : action.content}
						</BgtButton>
					))}
				</div>
			</div>
			<div className="block md:hidden">{children}</div>
		</div>
	);
};

export default BgtPageHeader;
