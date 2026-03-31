import type { ComponentType, PropsWithChildren, SVGProps } from "react";
import { BgtEmptyState } from "./BgtEmptyState";
import { BgtPage } from "./BgtPage";
import { BgtPageContent } from "./BgtPageContent";
import BgtPageHeader from "./BgtPageHeader";

interface Props extends PropsWithChildren {
	header?: string;
	icon?: ComponentType<SVGProps<SVGSVGElement>>;
	emptyIcon?: ComponentType<SVGProps<SVGSVGElement>>;
	title: string;
	description: string;
	action?: {
		label: string;
		onClick: () => void;
	};
	showHeader?: boolean;
}

export const BgtEmptyPage = (props: Props) => {
	const { header, icon, emptyIcon, title, description, action, children, showHeader = true } = props;

	const resolvedIcon = emptyIcon ?? icon;

	return (
		<BgtPage>
			{showHeader ? <BgtPageHeader header={header} icon={icon} /> : null}
			<BgtPageContent centered>
				{resolvedIcon && <BgtEmptyState icon={resolvedIcon} title={title} description={description} action={action} />}
				{children}
			</BgtPageContent>
		</BgtPage>
	);
};
