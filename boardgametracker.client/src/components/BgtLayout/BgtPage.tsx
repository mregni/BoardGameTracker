import { Children, type ReactElement } from "react";
import { BgtPageContent } from "./BgtPageContent";
import { BgtPageHeader } from "./BgtPageHeader";

interface Props {
	children: (ReactElement | null) | (ReactElement | null)[];
}

const checkComponentName = (
	child: ReactElement<unknown, string | React.JSXElementConstructor<unknown>>,
	elementName: string,
): boolean => {
	return (child?.type as (props: Props) => JSX.Element)?.name === elementName;
};

export const BgtPage = (props: Props) => {
	const { children } = props;

	let content: ReactElement | undefined;
	let header: ReactElement | undefined;

	Children.forEach(children, (child) => {
		if (child == null) {
			return;
		}

		if (checkComponentName(child, BgtPageHeader.name)) {
			header = child;
		} else if (checkComponentName(child, BgtPageContent.name)) {
			content = child;
		}
	});

	return (
		<div className="min-h-full flex flex-col p-3 xl:px-6 gap-3 xl:gap-6">
			{header}
			<div className="flex-1 flex flex-col min-h-0">{content}</div>
		</div>
	);
};
